using MediatR;

namespace Backend.Features.Reports.GetReportsByItem;

public record GetReportsByItemRequest(Guid ItemId) : IRequest<IResult>;

