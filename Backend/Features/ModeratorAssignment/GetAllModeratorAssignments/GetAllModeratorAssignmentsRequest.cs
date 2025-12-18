using MediatR;

namespace Backend.Features.ModeratorAssignment.GetAllModeratorAssignments;

public record GetAllModeratorAssignmentsRequest : IRequest<IResult>;
