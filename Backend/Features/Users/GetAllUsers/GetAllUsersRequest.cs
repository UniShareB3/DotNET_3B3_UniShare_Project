using MediatR;

namespace Backend.Features.Users.GetAllUsers;

public record GetAllUsersRequest() : IRequest<IResult>;
