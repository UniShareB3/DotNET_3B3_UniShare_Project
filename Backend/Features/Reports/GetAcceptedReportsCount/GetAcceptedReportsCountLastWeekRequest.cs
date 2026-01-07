using MediatR;

namespace Backend.Features.Reports.GetAcceptedReportsCount;

public record GetAcceptedReportsCountLastWeekRequest(Guid ItemId, int NumberOfDays) : IRequest<IResult>;

