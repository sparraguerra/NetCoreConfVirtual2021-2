using FormRecognizerFace.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;

namespace FormRecognizerFace.Fx
{
    public class GetBlobSasUriActivity
    {
        private readonly IBlobStorageRepository blobStorageRepository;

        public GetBlobSasUriActivity(IBlobStorageRepository blobStorageRepository)
        {
            this.blobStorageRepository = blobStorageRepository;
        }

        [FunctionName(nameof(GetBlobSasUriActivity))]
        public (Uri, string, string) RunGetBlobSasActivity([ActivityTrigger] string blobUrl)
        {
            var (containerName, blobName) = blobStorageRepository.GetContainerAndNameFromUri(blobUrl);

            return (blobStorageRepository.GetSasUri(containerName, blobName), containerName, blobName);
        }
    }
}
