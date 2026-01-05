using MediatR;

namespace Backend.Features.Reports.GetAcceptedReportsCount;

public record GetAcceptedReportsCountRequest(Guid ItemId, int NumberOfDays) : IRequest<IResult>;

