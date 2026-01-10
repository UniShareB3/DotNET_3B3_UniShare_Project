using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Services.AzureStorage;

public class AzureStorageService : IAzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger _logger = Log.ForContext<AzureStorageService>();

    public AzureStorageService(string connectionString, string containerName)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = containerName;
    }

    public string GenerateUploadSasUrl(string blobName, TimeSpan expiryTime)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                _logger.Warning("BlobClient cannot generate SAS URI for upload.");
                throw new InvalidOperationException("Cannot generate SAS token. Check storage account configuration.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiryTime)
            };

            // Permissions for upload: Create and Write
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            _logger.Information("Generated upload SAS URL for blob {BlobName} with expiry {ExpiryTime}", blobName, expiryTime);
            
            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating upload SAS URL for blob {BlobName}", blobName);
            throw;
        }
    }

    public string GenerateReadSasUrl(string blobName, TimeSpan expiryTime)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                _logger.Warning("BlobClient cannot generate SAS URI for read.");
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiryTime)
            };

            // Permission for read only
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            _logger.Information("Generated read SAS URL for blob {BlobName} with expiry {ExpiryTime}", blobName, expiryTime);
            
            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating read SAS URL for blob {BlobName}", blobName);
            throw;
        }
    }
}
