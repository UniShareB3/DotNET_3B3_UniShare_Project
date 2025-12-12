using Backend.Features.Users.Dtos;
using MediatR;

namespace Backend.Features.Users;

public record RegisterUserRequest(RegisterUserDto RegisterUserDto) : IRequest<IResult>;
