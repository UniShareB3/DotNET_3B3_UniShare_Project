using MediatR;

namespace Backend.Features.Users.GetAdminAccount;

public record GetAdminAccountRequest : IRequest<IResult>;

