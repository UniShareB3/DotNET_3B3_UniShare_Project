using Backend.Features.Reports.DTO;
using MediatR;

namespace Backend.Features.Reports.CreateReport;

public record CreateReportRequest(CreateReportDto Dto) : IRequest<IResult>;

