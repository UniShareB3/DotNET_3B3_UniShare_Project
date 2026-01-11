using Backend.Features.Conversations.DTO;
using MediatR;

namespace Backend.Features.Conversations.GetBulkDocumentUrls;

public record GetBulkDocumentUrlsRequest(GetBulkDocumentUrlsDto Dto) : IRequest<IResult>;

