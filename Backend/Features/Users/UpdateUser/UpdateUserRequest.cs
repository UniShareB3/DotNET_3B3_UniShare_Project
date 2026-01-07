using Backend.Features.Users.DTO;
using MediatR;

namespace Backend.Features.Users.UpdateUser;

public record UpdateUserRequest(Guid UserId, UpdateUserDto UpdateUserDto) : IRequest<IResult>;

