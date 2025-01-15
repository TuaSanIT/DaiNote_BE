using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace dai.api.Services.ServiceExtension
{
    public class AzureBlobService
    {
        BlobServiceClient _blobServiceClient;
        BlobContainerClient _blobContainerClient;
        string azureConnectionstring = "DefaultEndpointsProtocol=https;AccountName=dainoteblobv2;AccountKey=qOXovEpql/wvtaRtEY3jjndWl9sATJ/0xv0gpVav0voFWSV1W1utN36U1YMsXTnC2AgO9px0yCoB+AStuCuNIA==;EndpointSuffix=core.windows.net";

        public AzureBlobService()
        {
            _blobServiceClient = new BlobServiceClient(azureConnectionstring);
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("dainotecontainer");
        }

        public async Task<List<BlobContentInfo>> UploadFiles(List<IFormFile> files)
        {
            var azureResponse = new List<BlobContentInfo>();
            foreach (var file in files)
            {
                string fileName = file.FileName;
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    var client = await _blobContainerClient.UploadBlobAsync(fileName, memoryStream, default);
                    azureResponse.Add(client);
                }

            };
            return azureResponse;
        }

        public async Task<List<BlobItem>> GetUploadedBlob()
        {
            var items = new List<BlobItem>();
            var UploadedFiles = _blobContainerClient.GetBlobsAsync();
            await foreach (BlobItem file in UploadedFiles)
            {
                items.Add(file);
            }

            return items;
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string containerName, string folderName, string fileName)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(azureConnectionstring);

                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = blobContainerClient.GetBlobClient($"{folderName}/{fileName}");

                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = "image/jpeg" });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while uploading the image to Azure Blob Storage", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string oldImageUrl)
        {
            try
            {
                var blobName = Path.GetFileName(oldImageUrl);
                var containerClient = new BlobContainerClient(azureConnectionstring, "avatars");
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Failed to delete image: {ex.Message}");
                return false;
            }
        }



        public async Task<string> UploadTaskFileAsync(Stream fileStream, string containerName, string folderName, string fileName)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(azureConnectionstring);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = blobContainerClient.GetBlobClient($"{folderName}/{fileName}");

                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = "application/octet-stream" });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while uploading the file to Azure Blob Storage", ex);
            }
        }

        public async Task<bool> DeleteTaskFileAsync(string oldTaskUrl)
        {
            try
            {
                var blobName = Path.GetFileName(oldTaskUrl);
                var containerClient = new BlobContainerClient(azureConnectionstring, "task-files");
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Failed to delete task file: {ex.Message}");
                return false;
            }
        }
        public async Task<string> UploadFileAsync(Stream fileStream, string containerName, string folderName, string fileName, string contentType)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(azureConnectionstring);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = blobContainerClient.GetBlobClient($"{folderName}/{fileName}");

                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while uploading the file to Azure Blob Storage", ex);
            }
        }

        public async Task<string> UploadNoteImageAsync(Stream fileStream, string containerName, string folderName, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Ensure folderName is sanitized
                folderName = folderName.Trim('/').Replace("..", string.Empty);

                var blobClient = containerClient.GetBlobClient($"{folderName}/{fileName}");
                var contentType = GetContentType(fileName); // Dynamically determine MIME type
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while uploading the image to Azure Blob Storage", ex);
            }
        }

        public async Task<bool> DeleteNoteImageAsync(string oldImageUrl)
        {
            try
            {
                var uri = new Uri(oldImageUrl);
                var blobPath = uri.AbsolutePath.TrimStart('/'); // Extract full blob path

                var containerName = blobPath.Split('/')[0]; // Extract container name
                var blobName = string.Join('/', blobPath.Split('/').Skip(1)); // Extract blob path after container

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete image: {ex.Message}");
                return false;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream", // Default fallback
            };
        }
    }
}