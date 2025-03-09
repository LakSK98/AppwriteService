using AppWriteTest;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main()
    {
        // Configure AppWrite
        var endpoint = "https://cloud.appwrite.io/v1"; // Change if self-hosted
        var projectId = "projectId";
        var apiKey = "apiKey";
        var bucketId = "bucketId";

        // 1️⃣ Create a Service Collection (for Dependency Injection)
        var services = new ServiceCollection();

        // 2️⃣ Register IHttpClientFactory
        services.AddHttpClient("AppWriteClient");

        // 3️⃣ Register AppWriteService
        services.AddSingleton<AppWriteService>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return new AppWriteService(factory, projectId, apiKey, bucketId);
        });

        // 4️⃣ Build Service Provider
        var serviceProvider = services.BuildServiceProvider();

        // 5️⃣ Resolve AppWriteService & Call Method
        var appWriteService = serviceProvider.GetRequiredService<AppWriteService>();

        try
        {
            string filePath = "C:/Users/Lakshitha/Downloads/NET Roadmap.png"; // Specify the file path

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found!");
                return;
            }

            using var fileStream = File.OpenRead(filePath);
            string fileName = Path.GetFileName(filePath);
            string mimeType = GetMimeTypeFromPath(filePath);

            await appWriteService.UploadFileAsync(fileStream, fileName, mimeType);
            var files = await appWriteService.ListFilesAsync();
            await appWriteService.UpdateFileAsync(files[0].Id, "Updated file", files[0].MimeType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string GetMimeTypeFromPath(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (provider.TryGetContentType(filePath, out var contentType))
        {
            return contentType;
        }
        return "application/octet-stream"; // Default MIME type
    }
}

