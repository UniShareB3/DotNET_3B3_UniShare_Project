using MediatR;

namespace Backend.Features.Users;

public record GetAllUsersRequest() : IRequest<IResult>;
