using MediatR;

namespace Backend.Features.Users;

public record LoginUserRequest(string Email, string Password) : IRequest<IResult>;
