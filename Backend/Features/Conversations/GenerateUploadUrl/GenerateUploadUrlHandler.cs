using Backend.Features.Conversations.DTO;
using Backend.Services.AzureStorage;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.GenerateUploadUrl;

public class GenerateUploadUrlHandler(IAzureStorageService storageService) : IRequestHandler<GenerateUploadUrlRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GenerateUploadUrlHandler>();

    public async Task<IResult> Handle(GenerateUploadUrlRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Generating upload URL for file: {FileName}", request.FileName);

        try
        {
            var fileExtension = Path.GetExtension(request.FileName)?.ToLowerInvariant() ?? string.Empty;

            // Generate unique blob name
            var blobName = $"{Guid.NewGuid()}{fileExtension}";

            // Generate SAS URL for upload (valid for 15 minutes)
            var expiryTime = TimeSpan.FromMinutes(15);
            var uploadUrl = storageService.GenerateUploadSasUrl(blobName, expiryTime);

            var response = new GenerateUploadUrlResponse
            {
                UploadUrl = uploadUrl,
                BlobName = blobName,
                ExpiresAt = DateTime.UtcNow.Add(expiryTime)
            };

            _logger.Information("Generated upload URL for blob {BlobName}, expires at {ExpiresAt}", blobName, response.ExpiresAt);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating upload URL for file {FileName}", request.FileName);
            return Results.Problem("An unexpected error occurred while generating upload URL.");
        }
    }
}
