using MediatR;

namespace Backend.Features.Users;

public record ConfirmEmailRequest(Guid UserId, string Code) : IRequest<IResult>;
