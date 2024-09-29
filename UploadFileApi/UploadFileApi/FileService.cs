using Azure.Storage.Blobs;

namespace UploadFileApi
{
    //upload directly to azure
    //https://stackoverflow.com/questions/77062290/can-i-upload-a-file-from-the-users-browser-directly-to-azure-blob-storage-using

    //Performance profiler
    //https://www.youtube.com/watch?v=FpibK0PKfcI&list=PLReL099Y5nRf2cOurn1hI-gSRxsdbC27C&index=1
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

        public async Task<BlobResponseDto> UploadAsync(UploadFileRequestDto dto)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(dto.FileName);

            await blobClient.UploadAsync(dto.FilePath, overwrite: true);

            return new BlobResponseDto
            {
                Status = $"File {dto.FileName} Uploaded",
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
