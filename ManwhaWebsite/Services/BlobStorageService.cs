using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ManwhaWebsite.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration config)
        {
            var connStr = config["AzureStorage:ConnectionString"]!;
            var containerName = config["AzureStorage:ContainerName"] ?? "profile-pictures";
            _container = new BlobContainerClient(connStr, containerName);
            _container.CreateIfNotExists(PublicAccessType.None);
        }

        // Returns the blob name (not a URL) — use GetSasUrl to get a viewable URL
        public async Task<string> UploadAsync(string blobName, Stream content, string contentType)
        {
            var blob = _container.GetBlobClient(blobName);
            await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, conditions: null);
            return blobName;
        }

        public string GetSasUrl(string blobName, TimeSpan expiry)
        {
            var blob = _container.GetBlobClient(blobName);
            var sasUri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiry));
            return sasUri.ToString();
        }

        public async Task DeleteAsync(string blobName)
        {
            var blob = _container.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync();
        }
    }
}
