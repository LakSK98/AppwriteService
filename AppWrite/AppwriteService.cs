using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AppWriteTest
{
    public class AppWriteService
    {
        private readonly HttpClient _client;
        private readonly string _projectId;
        private readonly string _apiKey;
        private readonly string _bucketId;

        public AppWriteService(IHttpClientFactory httpClientFactory,
            string projectId, string apiKey, string bucketId, string endpoint = "https://cloud.appwrite.io/v1/")
        {
            _client = httpClientFactory.CreateClient("AppWriteClient");
            _client.BaseAddress = new Uri(endpoint); // For self-hosted, change this URL
            _projectId = projectId;
            _apiKey = apiKey;
            _bucketId = bucketId;
            // Set headers for the request
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-Appwrite-Project", _projectId);
            _client.DefaultRequestHeaders.Add("X-Appwrite-Key", _apiKey);
        }

        // Upload a file to the bucket
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                throw new Exception("File stream is empty!");
            }

            // Prepare the request content as Multipart Form Data
            var formContent = new MultipartFormDataContent
            {
                { new StreamContent(fileStream), "file", fileName },
                { new StringContent("unique()"), "fileId" }
            };

            try
            {
                // Send the file to the AppWrite API
                var response = await _client.PostAsync($"storage/buckets/{_bucketId}/files", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var fileData = JObject.Parse(responseBody);
                    return fileData["$id"]?.ToString();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error uploading file: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception: {ex.Message}");
            }
        }

        // Retrive file details
        public async Task<string> GetFileDetailsAsync(string fileId)
        {
            try
            {
                var response = await _client.GetAsync($"storage/buckets/{_bucketId}/files/{fileId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error retrieving file details: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception during file details retrieval", ex);
            }
        }

        // Method to update a file's metadata
        public async Task UpdateFileAsync(string fileId, string newFileName, string newMimeType)
        {
            var jsonContent = new JObject
            {
                ["name"] = newFileName,
                ["mimeType"] = newMimeType
            };

            try
            {
                var response = await _client.PutAsync($"storage/buckets/{_bucketId}/files/{fileId}", 
                    new StringContent(jsonContent.ToString(), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("File updated successfully.");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error updating file: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception during file update", ex);
            }
        }

        // Method to delete a file from the bucket
        public async Task DeleteFileAsync(string fileId)
        {
            try
            {
                var response = await _client.DeleteAsync($"storage/buckets/{_bucketId}/files/{fileId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error deleting file: {errorMessage}");
                }
                else
                {
                    Console.WriteLine("File deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception during file deletion", ex);
            }
        }

        // Method to list all files in the bucket
        public async Task<List<FileInfo>> ListFilesAsync()
        {
            try
            {
                var response = await _client.GetAsync($"storage/buckets/{_bucketId}/files");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JObject.Parse(responseBody);
                    var fileList = result["files"]?.Select(f => new FileInfo
                    {
                        Id = f["$id"]?.ToString(),
                        BucketId = f["bucketId"]?.ToString(),
                        CreatedAt = f["$createdAt"]?.ToString(),
                        UpdatedAt = f["$updatedAt"]?.ToString(),
                        Permissions = f["$permissions"]?.ToObject<List<string>>(),
                        Name = f["name"]?.ToString(),
                        Signature = f["signature"]?.ToString(),
                        MimeType = f["mimeType"]?.ToString(),
                        SizeOriginal = f["sizeOriginal"]?.ToObject<int>() ?? 0,
                        ChunksTotal = f["chunksTotal"]?.ToObject<int>() ?? 0,
                        ChunksUploaded = f["chunksUploaded"]?.ToObject<int>() ?? 0
                    }).ToList();
                    return fileList ?? new List<FileInfo>();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error listing files: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Exception during file listing", ex);
            }
        }

        // Method to get a file download URL
        public string GenerateFileDownloadLink(string fileId)
        {
            return $"https://cloud.appwrite.io/v1/storage/buckets/{_bucketId}/files/{fileId}/download?project={_projectId}";
        }

        // Method to get a file view URL
        public string GenerateFileViewLink(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new Exception("File ID is missing in the response.");
            }

            string fileUrl = $"https://cloud.appwrite.io/v1/storage/buckets/{_bucketId}/files/{fileId}/view?project={_projectId}";

            return fileUrl;
        }
    }


    public class FileInfo
    {
        public string Id { get; set; }
        public string BucketId { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public List<string> Permissions { get; set; }
        public string Name { get; set; }
        public string Signature { get; set; }
        public string MimeType { get; set; }
        public int SizeOriginal { get; set; }
        public int ChunksTotal { get; set; }
        public int ChunksUploaded { get; set; }
    }
}
