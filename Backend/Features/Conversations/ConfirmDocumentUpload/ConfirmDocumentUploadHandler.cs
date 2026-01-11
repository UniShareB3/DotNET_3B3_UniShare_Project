using Backend.Data;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using Backend.Services.ContentType;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.ConfirmDocumentUpload;

public class ConfirmDocumentUploadHandler(
    ApplicationContext dbContext,
    UserManager<User> userManager,
    IAzureStorageService storageService) : IRequestHandler<ConfirmDocumentUploadRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<ConfirmDocumentUploadHandler>();

    public async Task<IResult> Handle(ConfirmDocumentUploadRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Confirming document upload for blob {BlobName} from user {SenderId} to user {ReceiverId}",
            request.BlobName, request.SenderId, request.ReceiverId);

        try
        {
            // Verify sender exists
            var sender = await userManager.FindByIdAsync(request.SenderId.ToString());
            if (sender == null)
            {
                _logger.Warning("Sender {SenderId} not found", request.SenderId);
                return Results.NotFound("Sender not found");
            }

            // Verify receiver exists
            var receiver = await userManager.FindByIdAsync(request.ReceiverId.ToString());
            if (receiver == null)
            {
                _logger.Warning("Receiver {ReceiverId} not found", request.ReceiverId);
                return Results.NotFound("Receiver not found");
            }

            // Determine content type based on file extension
            var contentType = ContentTypeResolver.FromFileName(request.BlobName);

            // Create chat message (store only blob name, not the full URL)
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Caption ?? string.Empty,
                BlobName = request.BlobName,
                ContentType = contentType,
                Timestamp = DateTime.UtcNow
            };

            dbContext.ChatMessages.Add(chatMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Document upload confirmed and chat message created with ID {MessageId}", chatMessage.Id);

            // Generate a read URL for the response (using the constant expiry time)
            var readUrl = storageService.GenerateReadSasUrl(request.BlobName, BlobStorageConstants.ReadSasUrlExpiryTime);

            return Results.Ok(new
            {
                MessageId = chatMessage.Id,
                DocumentUrl = readUrl,
                BlobName = request.BlobName,
                Timestamp = chatMessage.Timestamp,
                ExpiresAt = DateTime.UtcNow.Add(BlobStorageConstants.ReadSasUrlExpiryTime)
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error confirming document upload for blob {BlobName}", request.BlobName);
            return Results.Problem("An unexpected error occurred while confirming document upload.");
        }
    }
}
