namespace Backend.Services.AzureStorage;

public static class BlobStorageConstants
{
    /// <summary>
    /// Default expiry time for read SAS URLs (1 hour)
    /// </summary>
    public static readonly TimeSpan ReadSasUrlExpiryTime = TimeSpan.FromHours(1);
}

