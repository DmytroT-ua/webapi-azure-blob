using Azure.Storage.Blobs;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace UploadFileApi
{
    //upload directly to azure
    //https://stackoverflow.com/questions/77062290/can-i-upload-a-file-from-the-users-browser-directly-to-azure-blob-storage-using
    public class FileService
    {
        private readonly IConfiguration configuration;
        private readonly BlobContainerClient blobContainerClient;

        public FileService(
            IConfiguration configuration)
        {
            this.configuration = configuration;

            string connectionString = configuration.GetValue<string>("AzureBlobStorage:ConnectionString");
            string filesContainerName = configuration.GetValue<string>("AzureBlobStorage:FilesContainerName");

            blobContainerClient = new BlobContainerClient(connectionString, filesContainerName);
        }

        public async Task<List<BlobDto>> ListAsync()
        {
            List<BlobDto> files = new();

            await foreach (var file in blobContainerClient.GetBlobsAsync())
            {
                string uri = blobContainerClient.Uri.ToString();
                string name = file.Name;
                string fullUri = $"{uri}/{name}";

                files.Add(new BlobDto
                {
                    Uri = fullUri,
                    Name = name,
                    ContentType = file.Properties.ContentType,
                    Version = file.VersionId,
                    IsLatest = file.IsLatestVersion ?? false,
                    Tier = file.Properties.AccessTier?.ToString()
                });
            }

            return files;
        }

        public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blob.FileName);

            await using (Stream data = blob.OpenReadStream())
            {
                await blobClient.UploadAsync(data, overwrite: true);
            }

            return new BlobResponseDto
            {
                Status = $"File {blob.FileName} Uploaded",
                Error = false,
                Blob = new BlobDto
                {
                    Uri = blobClient.Uri.AbsoluteUri,
                    Name = blobClient.Name
                }
            };
        }

        public async Task<BlobDto> DownloadAsync(string blobFileName)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobFileName);

            if (await blobClient.ExistsAsync())
            {
                var data = await blobClient.DownloadContentAsync();

                return new BlobDto
                {
                    Name = blobFileName,
                    ContentType = data.Value.Details.ContentType,
                    Content = data.Value.Content.ToStream(),
                };
            }

            return null;
        }

        public async Task<BlobResponseDto> DeleteAsync(string blobFileName)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobFileName);

            bool deleted = await blobClient.DeleteIfExistsAsync();

            return new BlobResponseDto
            {
                Status = deleted ? "Deleted" : "Not exists"
            };
        }
    }
}
