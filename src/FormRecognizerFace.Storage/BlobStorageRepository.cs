using Azure.Storage.Blobs;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FormRecognizerFace.Storage
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        readonly CloudBlobClient cloudBlobClient;
        readonly int minutesSasExpire;

        public BlobStorageRepository(IOptions<BlobStorageRepositoryOptions> configuration)
          : this(configuration.Value)
        {
        }

        public BlobStorageRepository(BlobStorageRepositoryOptions configuration)
        {
            ConnectionString = configuration.ConnectionString ?? throw new ArgumentException("connectionString");
            cloudBlobClient = GetClient();
            minutesSasExpire = configuration.MinutesSasExpire;
        }

        public string ConnectionString { get; private set; }

        public CloudBlobClient GetClient() => CloudStorageAccount.Parse(ConnectionString).CreateCloudBlobClient();

        public async Task<bool> CreateContainerAsync(string containerName, bool isPublic)
        {
            try
            {
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                if (isPublic)
                {
                    var exists = await cloudBlobContainer.CreateIfNotExistsAsync();
                    if (exists)
                    {
                        await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    }

                    return exists;
                }

                return await cloudBlobContainer.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} - Error while creating container.", ex);
            }
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobName);
                await blockBlob.DeleteAsync();
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobName} - Error while deleting blob.", ex);
            }
        }

        public async Task<bool> DeleteContainerAsync(string containerName)
        {
            try
            {
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                return await cloudBlobContainer.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} - Error while deleting container.", ex);
            }
        }

        public async Task<Stream> DownloadBlobAsStreamAsync(string containerName, string blobName)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobName);
                var context = PrepareTransfer();

                var memoryStream = new MemoryStream();
                await TransferManager.DownloadAsync(blockBlob, memoryStream, null, context, CancellationToken.None);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobName} - Error while downloading blob.", ex);
            }
        }

        public async Task<byte[]> DownloadBlobContentAsByteArrayAsync(string containerName, string blobName)
                        => (await DownloadBlobAsStreamAsync(containerName, blobName) as MemoryStream).ToArray();

        public async Task<string> DownloadBlobContentAsStringAsync(string containerName, string blobName)
            => Encoding.UTF8.GetString(await DownloadBlobContentAsByteArrayAsync(containerName, blobName));

        public IEnumerable<IListBlobItem> GetListBlobs(string containerName)
        {
            try
            {
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                return cloudBlobContainer.ListBlobs(null, false);
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} - Error while listing blobs.", ex);
            }
        }

        public async Task UploadBlobAsync(byte[] bytes, string containerName, string blobNameToCreate)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                await UploadBlobAsync(memoryStream, containerName, blobNameToCreate);
            }
        }

        public async Task UploadBlobAsync(Stream fileStream, string containerName, string blobNameToCreate)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobNameToCreate);
                var context = PrepareTransfer();

                await TransferManager.UploadAsync(fileStream, blockBlob, null, context, CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobNameToCreate} - Error while uploading blob.", ex);
            }
        }

        public Uri GetSasUri(string containerName, string blobName)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobName);
                var sasConstraints = new SharedAccessBlobPolicy
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(minutesSasExpire),
                    Permissions = SharedAccessBlobPermissions.Read
                };

                var sasContainerToken = blockBlob.GetSharedAccessSignature(sasConstraints);

                return new Uri($"{blockBlob.Uri}{sasContainerToken}");
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobName} - Error while getting Sas Uri blob.", ex);
            }
        }

        public (string, string) GetContainerAndNameFromUri(string blobUrl)
        {
            var blobUriBuilder = new BlobUriBuilder(new Uri(blobUrl));

            return (blobUriBuilder.BlobContainerName, blobUriBuilder.BlobName);
        }

        public async Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobName);
                await blockBlob.FetchAttributesAsync();

                return blockBlob.Metadata;
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobName} - Error while getting blob metadata.", ex);
            }
        }

        public async Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata)
        {
            try
            {
                var blockBlob = GetBlockBlob(containerName, blobName);

                blockBlob.Metadata.Clear();
                foreach (var item in metadata)
                {
                    blockBlob.Metadata.Add(item.Key, item.Value);
                }
                await blockBlob.SetMetadataAsync();
            }
            catch (Exception ex)
            {
                throw new BlobStorageException($"Container: {containerName} Blob: {blobName} - Error while setting blob metadata.", ex);
            }
        }

        CloudBlockBlob GetBlockBlob(string containerName, string blobName)
        {
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            var blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

            return blockBlob;
        }

        SingleTransferContext PrepareTransfer()
        {
            TransferManager.Configurations.ParallelOperations = ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 8;
            ServicePointManager.Expect100Continue = false;
            var context = new SingleTransferContext
            {
                LogLevel = LogLevel.Warning,
                ProgressHandler = new Progress<TransferStatus>((progress) =>
                {
                }),
                ShouldOverwriteCallbackAsync = TransferContext.ForceOverwrite
            };

            return context;
        }
    }
}
