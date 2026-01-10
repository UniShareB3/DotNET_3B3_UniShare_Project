using Backend.Data;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Conversations.ConfirmDocumentUpload;

public class ConfirmDocumentUploadHandler(
    ApplicationContext dbContext,
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
            var sender = await dbContext.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
            if (sender == null)
            {
                _logger.Warning("Sender {SenderId} not found", request.SenderId);
                return Results.NotFound("Sender not found");
            }

            // Verify receiver exists
            var receiver = await dbContext.Users.FindAsync(new object[] { request.ReceiverId }, cancellationToken);
            if (receiver == null)
            {
                _logger.Warning("Receiver {ReceiverId} not found", request.ReceiverId);
                return Results.NotFound("Receiver not found");
            }

            // Generate read SAS URL (valid for 7 days)
            var readUrl = storageService.GenerateReadSasUrl(request.BlobName, TimeSpan.FromDays(7));

            // Determine message type based on file extension
            var fileExtension = Path.GetExtension(request.BlobName)?.ToLowerInvariant();
            var messageType = fileExtension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => MessageType.Image,
                _ => MessageType.Text // For documents, we'll use Text type but with ImageUrl populated
            };

            // Create chat message
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Content = request.Caption ?? string.Empty,
                DocumentUrl = readUrl,
                MessageType = messageType,
                Timestamp = DateTime.UtcNow
            };

            dbContext.ChatMessages.Add(chatMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Document upload confirmed and chat message created with ID {MessageId}", chatMessage.Id);

            return Results.Ok(new
            {
                MessageId = chatMessage.Id,
                DocumentUrl = readUrl,
                Timestamp = chatMessage.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error confirming document upload for blob {BlobName}", request.BlobName);
            return Results.Problem("An unexpected error occurred while confirming document upload.");
        }
    }
}
