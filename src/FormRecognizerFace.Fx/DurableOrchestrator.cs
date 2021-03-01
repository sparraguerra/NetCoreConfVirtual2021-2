using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FormRecognizerFace.Fx
{
    public partial class DurableOrchestrator
    {
        [FunctionName("RunOrchestratorHttp")]
        public async Task<IActionResult> RunOrchestratorHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var url = JsonConvert.DeserializeObject<Dictionary<string, string>>(await req.Content.ReadAsStringAsync());
            var instanceId = await starter.StartNewAsync<string>("RunOrchestrator", System.Net.WebUtility.UrlEncode(url["url"]));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return new OkObjectResult("success");
        }

        [FunctionName("RunOrchestratorEventGrid")]
        public static async Task RunOrchestratorEventGrid(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"Start process.");

            log.LogInformation($"EventGridEvent data '{eventGridEvent.Data}'.");
            var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();

            var instanceId = await starter.StartNewAsync<string>("RunOrchestrator", createdEvent.Url);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }


        [FunctionName("RunOrchestrator")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var blobUrl = System.Net.WebUtility.UrlDecode(context.GetInput<string>());
            if (!string.IsNullOrWhiteSpace(blobUrl))
            {
                var (blobSasUri, containerName, blobName) = await GetSasUriAsync(context, log, blobUrl);

                var facturae = await GetFormRecognizerResultAsync(context, log, blobSasUri); 

                var signed = await GetFacturaeSignedAsync(context, log, facturae);

                _ = await UploadSignedDocumentAsync(context, log, signed, containerName, blobName);
            }
        }

        private async Task<string> GetFormRecognizerResultAsync(IDurableOrchestrationContext context, ILogger log, Uri blobSasUri)
        {
            // TODO: Execute FormRecognizer and get data
            log.LogInformation($"Orchestration {context.InstanceId}: Recognizing form and generating facturae object.");
            var formRecognizerResult = await context.CallActivityAsync<string>(nameof(GetFormRecognizerResultActivity), blobSasUri);
            log.LogInformation($"Orchestration {context.InstanceId}: Form recognized and retrieved data.");
            return formRecognizerResult;
        }

        private async Task<(Uri, string, string)> GetSasUriAsync(IDurableOrchestrationContext context, ILogger log, string blobUrl)
        {
            log.LogInformation($"Orchestration {context.InstanceId}: Getting blob Sas Uri from storage.");
            var blobSasUri = await context.CallActivityAsync<(Uri, string, string)>(nameof(GetBlobSasUriActivity), blobUrl);
            log.LogInformation($"Orchestration {context.InstanceId}: Sas Uri retrieved {blobSasUri}.");
            return blobSasUri;
        }
 
        private async Task<string> GetFacturaeSignedAsync(IDurableOrchestrationContext context, ILogger log,
                                       string facturae)
        { 
            log.LogInformation($"Orchestration {context.InstanceId}: Signing Facturae to XSIG.");
            var signedDocument = await context.CallActivityAsync<string>(nameof(GetFacturaeSignedActivity), facturae);
            log.LogInformation($"Orchestration {context.InstanceId}: Facturae signed (XSIG).");
            return signedDocument;
        }


        private async Task<bool> UploadSignedDocumentAsync(IDurableOrchestrationContext context, ILogger log,
                                                    string signedDocument, string containerName, string blobName)
        {
            var request = new UploadSignedDocumentModel()
            {
                ContainerName = containerName,
                BlobName = blobName,
                Content =  signedDocument
            };
            log.LogInformation($"Orchestration {context.InstanceId}: Uploading XSIG.");
            var result = await context.CallActivityAsync<bool>(nameof(UploadSignedDocumentActivity), request);
            log.LogInformation($"Orchestration {context.InstanceId}: XSIG Uploaded.");
            return result;
        }
    }
}