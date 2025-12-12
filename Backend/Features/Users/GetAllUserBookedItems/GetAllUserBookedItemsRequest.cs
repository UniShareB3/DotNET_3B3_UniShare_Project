using MediatR;

namespace Backend.Features.Users;

public record GetAllUserBookedItemsRequest(Guid UserId) : IRequest<IResult>;