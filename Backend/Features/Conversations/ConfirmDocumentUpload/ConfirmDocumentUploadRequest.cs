using MediatR;

namespace Backend.Features.Conversations.ConfirmDocumentUpload;

public record ConfirmDocumentUploadRequest(Guid SenderId, Guid ReceiverId, string BlobName, string? FileName, string? Caption) : IRequest<IResult>;

