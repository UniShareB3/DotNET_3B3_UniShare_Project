using Backend.Features.Conversations.DTO;
using MediatR;

namespace Backend.Features.Conversations.GetDocumentUrl;

public record GetDocumentUrlRequest(GetDocumentUrlDto Dto) : IRequest<IResult>;
