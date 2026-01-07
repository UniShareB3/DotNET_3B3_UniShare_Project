using Backend.Features.Users.DTO;
using MediatR;

namespace Backend.Features.Users.RegisterUser;

public record RegisterUserRequest(RegisterUserDto RegisterUserDto) : IRequest<IResult>;
