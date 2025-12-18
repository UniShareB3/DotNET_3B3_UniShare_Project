using Backend.Features.Reports.DTO;
using MediatR;

namespace Backend.Features.Reports.UpdateReportStatus;

public record UpdateReportStatusRequest(Guid ReportId, UpdateReportStatusDto Dto) : IRequest<IResult>;

