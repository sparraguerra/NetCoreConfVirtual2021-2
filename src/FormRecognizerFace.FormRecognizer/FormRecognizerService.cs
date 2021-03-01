using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FormRecognizerFace.FormRecognizer
{
    public class FormRecognizerService : IFormRecognizerService
    {
        private readonly FormRecognizerClient client;

        public FormRecognizerService(IOptions<FormRecognizerServiceOptions> configuration)
          : this(configuration.Value)
        {
        }

        public FormRecognizerService(FormRecognizerServiceOptions configuration)
        {
            var endpoint = configuration.Endpoint ?? throw new ArgumentException("endpoint");
            var apiKey = configuration.ApiKey ?? throw new ArgumentException("apiKey");
            client = new FormRecognizerClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<FormPageCollection> AnalyzeFormFromStream(Stream form) 
                    => await client.StartRecognizeContent(form).WaitForCompletionAsync();

        public async Task<RecognizedFormCollection> AnalyzeCustomFormFromStream(Stream form, string modelId) 
                    => await client.StartRecognizeCustomFormsAsync(modelId, form).WaitForCompletionAsync();

        public async Task<RecognizedFormCollection> AnalyzeCustomFormFromUri(Uri formUri, string modelId)
                    => await client.StartRecognizeCustomFormsFromUriAsync(modelId, formUri).WaitForCompletionAsync();

        public async Task<RecognizedFormCollection> AnalyzeReceiptFromStream(Stream receipt) 
                    => await client.StartRecognizeReceiptsAsync(receipt).WaitForCompletionAsync();        
    }
}
