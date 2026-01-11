using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.GetBulkDocumentUrls;

public class GetBulkDocumentUrlsHandler(
    ApplicationContext dbContext,
    IAzureStorageService storageService) : IRequestHandler<GetBulkDocumentUrlsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetBulkDocumentUrlsHandler>();

    public async Task<IResult> Handle(GetBulkDocumentUrlsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving bulk document URLs for {Count} blobs", request.Dto.BlobNames.Count);

        try
        {
            var blobNames = request.Dto.BlobNames.Distinct().ToList();

            if (blobNames.Count == 0)
            {
                _logger.Warning("Empty blob names list provided");
                return Results.BadRequest("At least one blob name is required");
            }

            // Find all messages with matching blob names
            var messages = await dbContext.ChatMessages
                .Where(m => m.BlobName != null && blobNames.Contains(m.BlobName))
                .ToListAsync(cancellationToken);

            var expiresAt = DateTime.UtcNow.Add(BlobStorageConstants.ReadSasUrlExpiryTime);
            var responses = new List<DocumentUrlResponseDto>();

            foreach (var blobName in blobNames)
            {
                // Find the message for this specific blob
                var message = messages.FirstOrDefault(m => m.BlobName == blobName);

                if (message == null)
                {
                    _logger.Warning("No message found with blob name: {BlobName}", blobName);
                    // Skip blobs that don't exist
                    continue;
                }

                try
                {
                    // Generate a fresh read SAS URL with 1 hour expiry
                    var documentUrl = storageService.GenerateReadSasUrl(blobName, BlobStorageConstants.ReadSasUrlExpiryTime);

                    responses.Add(new DocumentUrlResponseDto
                    {
                        DocumentUrl = documentUrl,
                        BlobName = blobName,
                        ContentType = message.ContentType,
                        ExpiresAt = expiresAt
                    });
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error generating SAS URL for blob {BlobName}", blobName);
                    // Continue processing other blobs even if one fails
                }
            }

            _logger.Information("Successfully generated {Count} document URLs out of {Total} requested", 
                responses.Count, blobNames.Count);

            return Results.Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving bulk document URLs");
            return Results.Problem("An unexpected error occurred while retrieving document URLs.");
        }
    }
}
