using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GetConversations;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Conversations;

public class GetConversationsHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    [Fact]
    public async Task Handle_UserWithConversations_ReturnsConversationsList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_Valid_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro"
        };

        var otherUser = new User
        {
            Id = otherUserId,
            FirstName = "Other",
            LastName = "User",
            Email = "other@student.uaic.ro"
        };

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Hello",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        context.Users.AddRange(currentUser, otherUser);
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().UserId.Should().Be(otherUserId);
        okResult.Value.First().UserName.Should().Be("Other User");
        okResult.Value.First().LastMessage.Should().Be("Hello");
    }

    [Fact]
    public async Task Handle_NoConversations_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_Empty_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleConversations_SortedByLastMessageTime()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_Sorted_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro"
        };

        var user1 = new User
        {
            Id = user1Id,
            FirstName = "User",
            LastName = "One",
            Email = "user1@student.uaic.ro"
        };

        var user2 = new User
        {
            Id = user2Id,
            FirstName = "User",
            LastName = "Two",
            Email = "user2@student.uaic.ro"
        };

        var olderMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = user1Id,
            Content = "Older message",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        };

        var newerMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = user2Id,
            ReceiverId = currentUserId,
            Content = "Newer message",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        context.Users.AddRange(currentUser, user1, user2);
        context.ChatMessages.AddRange(olderMessage, newerMessage);
        await context.SaveChangesAsync();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value.Should().HaveCount(2);
        okResult.Value![0].UserId.Should().Be(user2Id);
        okResult.Value[1].UserId.Should().Be(user1Id);
    }

    [Fact]
    public async Task Handle_IncludesSentAndReceivedMessages()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_SentReceived_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro"
        };

        var otherUser = new User
        {
            Id = otherUserId,
            FirstName = "Other",
            LastName = "User",
            Email = "other@student.uaic.ro"
        };

        var sentMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Sent",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        var receivedMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Received",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        context.Users.AddRange(currentUser, otherUser);
        context.ChatMessages.AddRange(sentMessage, receivedMessage);
        await context.SaveChangesAsync();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().LastMessage.Should().Be("Received");
    }

    [Fact]
    public async Task Handle_IncludesLastMessageSenderId()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_SenderId_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro"
        };

        var otherUser = new User
        {
            Id = otherUserId,
            FirstName = "Other",
            LastName = "User",
            Email = "other@student.uaic.ro"
        };

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Hello",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        context.Users.AddRange(currentUser, otherUser);
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value!.First().LastMessageSenderId.Should().Be(otherUserId);
    }

    [Fact]
    public async Task Handle_SkipsDeletedUsers()
    {
        // Arrange
        var context = CreateInMemoryDbContext("Conversations_DeletedUser_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var deletedUserId = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            FirstName = "Current",
            LastName = "User",
            Email = "current@student.uaic.ro"
        };

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = deletedUserId,
            Content = "Hello",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        context.Users.Add(currentUser);
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(currentUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ConversationDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsProblem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("Conversations_DbException_" + Guid.NewGuid())
            .Options;

        var context = new ApplicationContext(options);
        var handler = new GetConversationsHandler(context);
        var request = new GetConversationsRequest(Guid.NewGuid());

        // Dispose context to simulate database error
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
