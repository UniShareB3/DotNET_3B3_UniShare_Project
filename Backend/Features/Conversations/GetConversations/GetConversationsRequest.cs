using MediatR;

namespace Backend.Features.Conversations.GetConversations;

public record GetConversationsRequest(Guid CurrentUserId) : IRequest<IResult>;

