using Backend.Features.Users.Dtos;
using MediatR;

namespace Backend.Features.Users;

public record UpdateUserRequest(Guid UserId, UpdateUserDto UpdateUserDto) : IRequest<IResult>;

