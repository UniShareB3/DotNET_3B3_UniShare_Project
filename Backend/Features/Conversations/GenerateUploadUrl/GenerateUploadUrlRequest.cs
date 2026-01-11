using Backend.Features.Conversations.DTO;
using MediatR;

namespace Backend.Features.Conversations.GenerateUploadUrl;

public record GenerateUploadUrlRequest(GenerateUploadUrlDto Dto) : IRequest<IResult>;
