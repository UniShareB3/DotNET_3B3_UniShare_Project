namespace Backend.Services.AzureStorage;

public interface IAzureStorageService
{
    string GenerateUploadSasUrl(string blobName, TimeSpan expiryTime);
    string GenerateReadSasUrl(string blobName, TimeSpan expiryTime);
}

