using FormRecognizerFace.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

namespace FormRecognizerFace.Fx
{
    public class UploadSignedDocumentModel
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
        public string Content { get; set; }
    }

    public class UploadSignedDocumentActivity
    {
        private readonly IBlobStorageRepository blobStorageRepository;

        public UploadSignedDocumentActivity(IBlobStorageRepository blobStorageRepository)
        {
            this.blobStorageRepository = blobStorageRepository;
        }

        [FunctionName(nameof(UploadSignedDocumentActivity))]
        public async Task<bool> RunUploadSignedDocumentActivity([ActivityTrigger] UploadSignedDocumentModel request)
        {
            await blobStorageRepository.UploadBlobAsync(System.Convert.FromBase64String(request.Content), 
                                                        $"{request.ContainerName}/signedDocuments", 
                                                        $"{request.BlobName}.xsig");
            return true;
        }
    }
}
