using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GetDocumentUrl;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Conversations;

public class GetDocumentUrlHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    private static Mock<IAzureStorageService> CreateMockStorageService()
    {
        var mock = new Mock<IAzureStorageService>();
        mock.Setup(s => s.GenerateReadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns((string blobName, TimeSpan _) => $"https://storage.blob.core.windows.net/container/{blobName}?sas=token");
        return mock;
    }

    [Fact]
    public async Task Handle_ExistingBlob_ReturnsDocumentUrl()
    {
        // Arrange
        var context = CreateInMemoryDbContext("DocumentUrl_Valid_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "Check this document",
            BlobName = "document.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "document.pdf" };
        var request = new GetDocumentUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<DocumentUrlResponseDto>>().Subject;
        okResult.Value!.BlobName.Should().Be("document.pdf");
        okResult.Value.ContentType.Should().Be("application/pdf");
        okResult.Value.DocumentUrl.Should().Contain("document.pdf");
        okResult.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_NonExistentBlob_ReturnsNotFound()
    {
        // Arrange
        var context = CreateInMemoryDbContext("DocumentUrl_NotFound_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "nonexistent.pdf" };
        var request = new GetDocumentUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFound<string>>().Subject;
        notFoundResult.Value.Should().Be("Document not found");
    }

    [Fact]
    public async Task Handle_ImageBlob_ReturnsCorrectContentType()
    {
        // Arrange
        var context = CreateInMemoryDbContext("DocumentUrl_Image_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "Check this image",
            BlobName = "photo.jpg",
            ContentType = "image/jpeg",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "photo.jpg" };
        var request = new GetDocumentUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<DocumentUrlResponseDto>>().Subject;
        okResult.Value!.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Handle_StorageServiceCalled_WithCorrectParameters()
    {
        // Arrange
        var context = CreateInMemoryDbContext("DocumentUrl_ServiceCall_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "Document",
            BlobName = "test.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "test.pdf" };
        var request = new GetDocumentUrlRequest(dto);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        storageServiceMock.Verify(s => s.GenerateReadSasUrl("test.pdf", BlobStorageConstants.ReadSasUrlExpiryTime), Times.Once);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsProblem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("DocumentUrl_DbException_" + Guid.NewGuid())
            .Options;

        var context = new ApplicationContext(options);
        var storageServiceMock = CreateMockStorageService();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "document.pdf" };
        var request = new GetDocumentUrlRequest(dto);

        // Dispose context to simulate database error
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Handle_StorageServiceException_ReturnsProblem()
    {
        // Arrange
        var context = CreateInMemoryDbContext("DocumentUrl_StorageException_" + Guid.NewGuid());
        var storageServiceMock = new Mock<IAzureStorageService>();
        storageServiceMock.Setup(s => s.GenerateReadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Throws(new Exception("Storage service error"));

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "Document",
            BlobName = "test.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetDocumentUrlHandler(context, storageServiceMock.Object);
        var dto = new GetDocumentUrlDto { BlobName = "test.pdf" };
        var request = new GetDocumentUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
