using MediatR;

namespace Backend.Features.Users.GetAdmins;

public record GetAdminsRequest : IRequest<IResult>;

