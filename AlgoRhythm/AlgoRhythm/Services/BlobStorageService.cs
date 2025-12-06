using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AlgoRhythm.Services;
public class BlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["AzureStorage:ContainerName"] ?? "azurite";
    }

    /// <summary>
    /// Saveds file (stream) in blob
    /// </summary>
    /// <param name="identifier">A name to save file with</param>
    /// <param name="content">File stream (eg. from IFormFile).</param>
    /// <param name="contentType">Type (eg. image/jpeg).</param>
    public async Task<string> UploadFileAsync(string identifier, Stream content, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(identifier);
        
        var conditions = new BlobRequestConditions { IfNoneMatch = ETag.All };
        
        var httpHeaders = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(
            content, 
            new BlobUploadOptions 
            { 
                HttpHeaders = httpHeaders,
                Conditions = conditions,
            });

        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Retrieves video data from blob
    /// </summary>
    public async Task<LectureVideo?> GetVideoInfoAsync(string identifier)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(identifier);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync();

            string streamUrl = GetStreamUrl(identifier);

            return new LectureVideo
            {
                FileName = identifier,
                StreamUrl = streamUrl,
                FileSize = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified.DateTime
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string GetStreamUrl(string identifier)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(identifier);

        // SAS token for 1 hour access
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = identifier,
            Resource = "b", // "b" = blob
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Uri with SAS token
        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        return sasUri.ToString();
    }

    /// <summary>
    /// Returns a file stream from blob
    /// </summary>
    public async Task<(Stream stream, string contentType)> GetFileAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob '{blobName}' not found.");
        }

        var properties = await blobClient.GetPropertiesAsync();
        string contentType = properties.Value.ContentType ?? "application/octet-stream";

        // Temp file for optimizations
        string tempFilePath = Path.GetRandomFileName();

        await blobClient.DownloadToAsync(tempFilePath);

        // Open and delete file on close
        var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

        return (fileStream, contentType);
    }

    public async Task<bool> DeleteFileAsync(string identifier)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(identifier);

        return (await blobClient.DeleteIfExistsAsync()).Value;
    }
}