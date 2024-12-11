using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace dai.api.Services.ServiceExtension
{
    public class AzureBlobService
    {
        BlobServiceClient _blobServiceClient;
        BlobContainerClient _blobContainerClient;
        string azureConnectionstring = "DefaultEndpointsProtocol=https;AccountName=dainoteblob;AccountKey=LVxaLYY8aWvXmxeE9HOpuvkB3Wco/8IwXSk8Yg4xIifasdWw6yJTCUvlJB8rY5A2pfxl+/mxSlNd+AStWCRHuw==;EndpointSuffix=core.windows.net";

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
            await foreach(BlobItem file in UploadedFiles)
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
                // Log the error
                Console.Error.WriteLine($"Failed to delete image: {ex.Message}");
                return false;
            }
        }


        //Upload task file
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
                // Log the error
                Console.Error.WriteLine($"Failed to delete task file: {ex.Message}");
                return false;
            }
        }


    }
}
