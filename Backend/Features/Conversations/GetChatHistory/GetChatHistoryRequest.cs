using MediatR;

namespace Backend.Features.Conversations.GetChatHistory;

public record GetChatHistoryRequest(Guid CurrentUserId, Guid OtherUserId) : IRequest<IResult>;

