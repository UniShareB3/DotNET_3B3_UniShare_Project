using Backend.Data;
using Backend.Features.Conversations.ConfirmDocumentUpload;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Conversations;

public class ConfirmDocumentUploadHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    private static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<IAzureStorageService> CreateMockStorageService()
    {
        var mock = new Mock<IAzureStorageService>();
        mock.Setup(s => s.GenerateReadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns((string blobName, TimeSpan _) => $"https://storage.blob.core.windows.net/container/{blobName}?sas=token");
        return mock;
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesMessageAndReturnsOk()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ConfirmUpload_Valid_" + Guid.NewGuid());
        var userManagerMock = CreateMockUserManager();
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var sender = new User
        {
            Id = senderId,
            FirstName = "Sender",
            LastName = "User",
            Email = "sender@student.uaic.ro"
        };

        var receiver = new User
        {
            Id = receiverId,
            FirstName = "Receiver",
            LastName = "User",
            Email = "receiver@student.uaic.ro"
        };

        userManagerMock.Setup(um => um.FindByIdAsync(senderId.ToString()))
            .ReturnsAsync(sender);
        userManagerMock.Setup(um => um.FindByIdAsync(receiverId.ToString()))
            .ReturnsAsync(receiver);

        var handler = new ConfirmDocumentUploadHandler(context, userManagerMock.Object, storageServiceMock.Object);
        var request = new ConfirmDocumentUploadRequest(senderId, receiverId, "test-document.pdf", "Test caption", "fileName1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var message = await context.ChatMessages.FirstOrDefaultAsync();
        message.Should().NotBeNull();
        message!.SenderId.Should().Be(senderId);
        message.ReceiverId.Should().Be(receiverId);
        message.BlobName.Should().Be("test-document.pdf");
        message.Content.Should().Be("Test caption");
    }

    [Fact]
    public async Task Handle_NullCaption_CreatesMessageWithEmptyContent()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ConfirmUpload_NullCaption_" + Guid.NewGuid());
        var userManagerMock = CreateMockUserManager();
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var sender = new User
        {
            Id = senderId,
            FirstName = "Sender",
            LastName = "User",
            Email = "sender@student.uaic.ro"
        };

        var receiver = new User
        {
            Id = receiverId,
            FirstName = "Receiver",
            LastName = "User",
            Email = "receiver@student.uaic.ro"
        };

        userManagerMock.Setup(um => um.FindByIdAsync(senderId.ToString()))
            .ReturnsAsync(sender);
        userManagerMock.Setup(um => um.FindByIdAsync(receiverId.ToString()))
            .ReturnsAsync(receiver);

        var handler = new ConfirmDocumentUploadHandler(context, userManagerMock.Object, storageServiceMock.Object);
        var request = new ConfirmDocumentUploadRequest(senderId, receiverId, "image.jpg", null, "fileName1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var message = await context.ChatMessages.FirstOrDefaultAsync();
        message.Should().NotBeNull();
        message!.Content.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_SenderNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ConfirmUpload_SenderNotFound_" + Guid.NewGuid());
        var userManagerMock = CreateMockUserManager();
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        userManagerMock.Setup(um => um.FindByIdAsync(senderId.ToString()))
            .ReturnsAsync((User?)null);

        var handler = new ConfirmDocumentUploadHandler(context, userManagerMock.Object, storageServiceMock.Object);
        var request = new ConfirmDocumentUploadRequest(senderId, receiverId, "test-document.pdf", "Caption", "fileName1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFound<string>>().Subject;
        notFoundResult.Value.Should().Be("Sender not found");
    }

    [Fact]
    public async Task Handle_ReceiverNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("ConfirmUpload_ReceiverNotFound_" + Guid.NewGuid());
        var userManagerMock = CreateMockUserManager();
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var sender = new User
        {
            Id = senderId,
            FirstName = "Sender",
            LastName = "User",
            Email = "sender@student.uaic.ro"
        };

        userManagerMock.Setup(um => um.FindByIdAsync(senderId.ToString()))
            .ReturnsAsync(sender);
        userManagerMock.Setup(um => um.FindByIdAsync(receiverId.ToString()))
            .ReturnsAsync((User?)null);

        var handler = new ConfirmDocumentUploadHandler(context, userManagerMock.Object, storageServiceMock.Object);
        var request = new ConfirmDocumentUploadRequest(senderId, receiverId, "test-document.pdf", "Caption", "fileName1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFound<string>>().Subject;
        notFoundResult.Value.Should().Be("Receiver not found");
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsProblem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("ConfirmUpload_DbException_" + Guid.NewGuid())
            .Options;

        var context = new ThrowingDbContext(options);
        var userManagerMock = CreateMockUserManager();
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var sender = new User
        {
            Id = senderId,
            FirstName = "Sender",
            LastName = "User",
            Email = "sender@student.uaic.ro"
        };

        var receiver = new User
        {
            Id = receiverId,
            FirstName = "Receiver",
            LastName = "User",
            Email = "receiver@student.uaic.ro"
        };

        userManagerMock.Setup(um => um.FindByIdAsync(senderId.ToString()))
            .ReturnsAsync(sender);
        userManagerMock.Setup(um => um.FindByIdAsync(receiverId.ToString()))
            .ReturnsAsync(receiver);

        var handler = new ConfirmDocumentUploadHandler(context, userManagerMock.Object, storageServiceMock.Object);
        var request = new ConfirmDocumentUploadRequest(senderId, receiverId, "test-document.pdf", "Caption", "fileName1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private class ThrowingDbContext(DbContextOptions<ApplicationContext> options) : ApplicationContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated database error");
        }
    }
}
