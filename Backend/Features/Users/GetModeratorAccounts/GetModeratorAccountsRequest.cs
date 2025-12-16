using MediatR;

namespace Backend.Features.Users.GetModeratorAccounts;

public record GetModeratorAccountsRequest : IRequest<IResult>;

