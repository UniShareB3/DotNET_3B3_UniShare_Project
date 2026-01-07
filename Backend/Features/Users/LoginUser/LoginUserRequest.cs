using MediatR;

namespace Backend.Features.Users.LoginUser;

public record LoginUserRequest(string Email, string Password) : IRequest<IResult>;
