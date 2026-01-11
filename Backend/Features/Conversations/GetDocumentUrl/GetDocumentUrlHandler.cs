using Backend.Features.Conversations.DTO;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.GetDocumentUrl;

public class GetDocumentUrlHandler(
    ApplicationContext dbContext,
    IAzureStorageService storageService) : IRequestHandler<GetDocumentUrlRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetDocumentUrlHandler>();

    public async Task<IResult> Handle(GetDocumentUrlRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Retrieving document URL for blob: {BlobName}", request.Dto.BlobName);

        try
        {
            // Find the message with this exact blob name
            var message = await dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.BlobName == request.Dto.BlobName,
                    cancellationToken);

            if (message == null)
            {
                _logger.Warning("No message found with blob name: {BlobName}", request.Dto.BlobName);
                return Results.NotFound("Document not found");
            }

            // Generate a fresh read SAS URL with 1 hour expiry
            var documentUrl = storageService.GenerateReadSasUrl(request.Dto.BlobName, BlobStorageConstants.ReadSasUrlExpiryTime);
            var expiresAt = DateTime.UtcNow.Add(BlobStorageConstants.ReadSasUrlExpiryTime);

            _logger.Information("Successfully generated document URL for blob: {BlobName}", request.Dto.BlobName);

            var response = new DocumentUrlResponseDto
            {
                DocumentUrl = documentUrl,
                BlobName = request.Dto.BlobName,
                ContentType = message.ContentType,
                ExpiresAt = expiresAt
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving document URL for blob {BlobName}", request.Dto.BlobName);
            return Results.Problem("An unexpected error occurred while retrieving document URL.");
        }
    }
}
