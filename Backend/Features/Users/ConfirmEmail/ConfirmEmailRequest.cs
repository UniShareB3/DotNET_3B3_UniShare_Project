using MediatR;

namespace Backend.Features.Users.ConfirmEmail;

public record ConfirmEmailRequest(Guid UserId, string Code) : IRequest<IResult>;
