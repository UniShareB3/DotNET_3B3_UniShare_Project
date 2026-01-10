using MediatR;

namespace Backend.Features.Conversations.GenerateUploadUrl;

public record GenerateUploadUrlRequest(string FileName, string ContentType) : IRequest<IResult>;

