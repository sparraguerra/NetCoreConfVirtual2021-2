using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FormRecognizerFace.Storage
{
    public interface IBlobStorageRepository
    {
        Task<bool> CreateContainerAsync(string containerName, bool isPublic);

        Task DeleteBlobAsync(string containerName, string blobName);

        Task<bool> DeleteContainerAsync(string containerName);

        Task<Stream> DownloadBlobAsStreamAsync(string containerName, string blobName);

        Task<string> DownloadBlobContentAsStringAsync(string containerName, string blobName);

        Task<byte[]> DownloadBlobContentAsByteArrayAsync(string containerName, string blobName);

        IEnumerable<IListBlobItem> GetListBlobs(string containerName);

        Task UploadBlobAsync(Stream fileStream, string containerName, string blobNameToCreate);

        Task UploadBlobAsync(byte[] bytes, string containerName, string blobNameToCreate);

        Uri GetSasUri(string containerName, string blobName);

        (string, string) GetContainerAndNameFromUri(string blobUrl);

        Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName);

        Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata);
    }
}
