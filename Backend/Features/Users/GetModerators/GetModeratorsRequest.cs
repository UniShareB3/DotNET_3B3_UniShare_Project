using MediatR;

namespace Backend.Features.Users.GetModerators;

public record GetModeratorsRequest : IRequest<IResult>;

