using AlgoRhythm.Services.Blob.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Dtos.Common;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AlgoRhythm.Services.Blob;
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
    /// Saves file (stream) in blob with unique identifier
    /// </summary>
    /// <param name="identifier">A name to save file with</param>
    /// <param name="content">File stream (eg. from IFormFile).</param>
    /// <param name="contentType">Type (eg. image/jpeg).</param>
    public async Task<string> UploadFileAsync(string identifier, Stream content, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // Add GUID prefix to ensure uniqueness while keeping original filename
        string uniqueIdentifier = $"{Guid.NewGuid()}_{identifier}";
        var blobClient = containerClient.GetBlobClient(uniqueIdentifier);
        
        var httpHeaders = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(
            content, 
            new BlobUploadOptions 
            { 
                HttpHeaders = httpHeaders
            });

        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Lists files in blob storage with pagination
    /// </summary>
    /// <param name="pageSize">Number of files to return per page</param>
    /// <param name="continuationToken">Token for next page (null for first page)</param>
    /// <returns>List of files and continuation token for next page</returns>
    public async Task<(List<BlobFileInfo> files, string? continuationToken)> ListFilesAsync(int pageSize = 50, string? continuationToken = null)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        
        if (!await containerClient.ExistsAsync())
        {
            return (new List<BlobFileInfo>(), null);
        }

        var files = new List<BlobFileInfo>();
        
        var resultSegment = containerClient.GetBlobsAsync(BlobTraits.Metadata)
            .AsPages(continuationToken, pageSize);

        await foreach (var blobPage in resultSegment)
        {
            foreach (var blobItem in blobPage.Values)
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                
                // Extract original filename (remove GUID prefix)
                string originalFileName = blobItem.Name;
                int underscoreIndex = blobItem.Name.IndexOf('_');
                if (underscoreIndex > 0 && underscoreIndex == 36) // GUID length
                {
                    originalFileName = blobItem.Name.Substring(underscoreIndex + 1);
                }

                files.Add(new BlobFileInfo
                {
                    FileName = blobItem.Name,
                    OriginalFileName = originalFileName,
                    ContentType = blobItem.Properties.ContentType ?? "application/octet-stream",
                    SizeInBytes = blobItem.Properties.ContentLength ?? 0,
                    LastModified = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue,
                    Url = blobClient.Uri.ToString()
                });
            }

            // Return after first page
            return (files, blobPage.ContinuationToken);
        }

        return (files, null);
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