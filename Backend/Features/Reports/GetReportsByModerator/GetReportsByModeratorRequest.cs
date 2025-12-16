using MediatR;

namespace Backend.Features.Reports.GetReportsByModerator;

public record GetReportsByModeratorRequest(Guid ModeratorId) : IRequest<IResult>;

