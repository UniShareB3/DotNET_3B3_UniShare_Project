using MediatR;

namespace Backend.Features.Users;

public record ConfirmEmailRequest(string JwtToken, string Code) : IRequest<IResult>;
