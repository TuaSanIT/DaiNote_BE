using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace dai.api.Services.ServiceExtension
{
    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;

        public AzureBlobService()
        {
            // Sử dụng Managed Identity qua DefaultAzureCredential
            var credential = new DefaultAzureCredential();
            _blobServiceClient = new BlobServiceClient(new Uri("https://dainoteblob.blob.core.windows.net"), credential);
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("dainotecontainer");
        }

        // Upload danh sách tệp
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

                    var client = await _blobContainerClient.UploadBlobAsync(fileName, memoryStream);
                    azureResponse.Add(client.Value);
                }
            }
            return azureResponse;
        }

        // Lấy danh sách Blob đã tải lên
        public async Task<List<BlobItem>> GetUploadedBlob()
        {
            var items = new List<BlobItem>();
            var uploadedFiles = _blobContainerClient.GetBlobsAsync();
            await foreach (BlobItem file in uploadedFiles)
            {
                items.Add(file);
            }
            return items;
        }

        // Upload một hình ảnh
        public async Task<string> UploadImageAsync(Stream fileStream, string containerName, string folderName, string fileName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
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

        // Xóa một Blob
        public async Task<bool> DeleteFileAsync(string oldImageUrl)
        {
            try
            {
                var blobName = Path.GetFileName(oldImageUrl);
                var containerClient = new BlobContainerClient(new Uri("https://dainoteblob.blob.core.windows.net/avatars"), new DefaultAzureCredential());
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.Error.WriteLine($"Failed to delete image: {ex.Message}");
                return false;
            }
        }

        // Upload một tệp nhiệm vụ
        public async Task<string> UploadTaskFileAsync(Stream fileStream, string containerName, string folderName, string fileName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
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

        // Xóa một tệp nhiệm vụ
        public async Task<bool> DeleteTaskFileAsync(string oldTaskUrl)
        {
            try
            {
                var blobName = Path.GetFileName(oldTaskUrl);
                var containerClient = new BlobContainerClient(new Uri("https://dainoteblob.blob.core.windows.net/task-files"), new DefaultAzureCredential());
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.Error.WriteLine($"Failed to delete task file: {ex.Message}");
                return false;
            }
        }
    }
}
