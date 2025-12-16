using MediatR;

namespace Backend.Features.Reports.GetAllReports;

public record GetAllReportsRequest : IRequest<IResult>;

