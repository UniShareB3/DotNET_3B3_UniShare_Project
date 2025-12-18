using Backend.Features.ModeratorAssignment.DTO;
using MediatR;

namespace Backend.Features.ModeratorAssignment.CreateModeratorAssignment;

public record CreateModeratorAssignmentRequest(CreateModeratorAssignmentDto Dto) : IRequest<IResult>;
