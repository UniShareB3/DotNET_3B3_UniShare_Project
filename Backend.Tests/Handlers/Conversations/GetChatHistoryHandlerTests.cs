using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GetChatHistory;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Handlers.Conversations;

public class GetChatHistoryHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    [Fact]
    public async Task Handle_ValidUsers_ReturnsMessages()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ChatHistory_Valid_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var message1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Hello",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        var message2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Hi there",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        context.ChatMessages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(currentUserId, otherUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ChatMessageDto>>>().Subject;
        okResult.Value.Should().HaveCount(2);
        okResult.Value!.First().Content.Should().Be("Hello");
        okResult.Value.Last().Content.Should().Be("Hi there");
    }

    [Fact]
    public async Task Handle_NoMessages_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ChatHistory_Empty_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(currentUserId, otherUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ChatMessageDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MessagesOrderedByTimestamp()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ChatHistory_Ordered_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var message1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Third",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "First",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddMinutes(-20)
        };

        var message3 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Second",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        context.ChatMessages.AddRange(message1, message2, message3);
        await context.SaveChangesAsync();

        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(currentUserId, otherUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ChatMessageDto>>>().Subject;
        okResult.Value.Should().HaveCount(3);
        okResult.Value![0].Content.Should().Be("First");
        okResult.Value[1].Content.Should().Be("Second");
        okResult.Value[2].Content.Should().Be("Third");
    }

    [Fact]
    public async Task Handle_OnlyReturnsMessagesForSpecifiedUsers()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ChatHistory_FilteredUsers_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var thirdUserId = Guid.NewGuid();

        var message1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Message between current and other",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = thirdUserId,
            Content = "Message between current and third",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        var message3 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = thirdUserId,
            Content = "Message between other and third",
            ContentType = "text/plain",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.AddRange(message1, message2, message3);
        await context.SaveChangesAsync();

        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(currentUserId, otherUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ChatMessageDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().Content.Should().Be("Message between current and other");
    }

    [Fact]
    public async Task Handle_IncludesMessagesWithBlobName()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ChatHistory_WithBlob_" + Guid.NewGuid());
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = otherUserId,
            Content = "Check this file",
            BlobName = "document.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(currentUserId, otherUserId);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<ChatMessageDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().BlobName.Should().Be("document.pdf");
        okResult.Value.First().ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsProblem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("ChatHistory_DbException_" + Guid.NewGuid())
            .Options;

        var context = new ApplicationContext(options);
        var handler = new GetChatHistoryHandler(context);
        var request = new GetChatHistoryRequest(Guid.NewGuid(), Guid.NewGuid());

        // Dispose context to simulate database error
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
