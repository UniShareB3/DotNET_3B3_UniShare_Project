using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GetBulkDocumentUrls;
using Backend.Persistence;
using Backend.Services.AzureStorage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Conversations;

public class GetBulkDocumentUrlsHandlerTests
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
    public async Task Handle_ValidBlobNames_ReturnsDocumentUrls()
    {
        // Arrange
        var context = CreateInMemoryDbContext("BulkUrls_Valid_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "File 1",
            BlobName = "blob1.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "File 2",
            BlobName = "blob2.jpg",
            ContentType = "image/jpeg",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { "blob1.pdf", "blob2.jpg" } };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<DocumentUrlResponseDto>>>().Subject;
        okResult.Value.Should().HaveCount(2);
        okResult.Value!.Should().Contain(r => r.BlobName == "blob1.pdf");
        okResult.Value.Should().Contain(r => r.BlobName == "blob2.jpg");
    }

    [Fact]
    public async Task Handle_EmptyBlobNames_ReturnsBadRequest()
    {
        // Arrange
        var context = CreateInMemoryDbContext("BulkUrls_Empty_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string>() };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequest<string>>().Subject;
        badRequestResult.Value.Should().Be("At least one blob name is required");
    }

    [Fact]
    public async Task Handle_NonExistentBlobs_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("BulkUrls_NonExistent_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { "nonexistent.pdf" } };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<DocumentUrlResponseDto>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DuplicateBlobNames_ReturnsDistinctUrls()
    {
        // Arrange
        var context = CreateInMemoryDbContext("BulkUrls_Duplicates_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "File",
            BlobName = "blob.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { "blob.pdf", "blob.pdf", "blob.pdf" } };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<DocumentUrlResponseDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_PartialMatch_ReturnsOnlyExistingBlobs()
    {
        // Arrange
        var context = CreateInMemoryDbContext("BulkUrls_Partial_" + Guid.NewGuid());
        var storageServiceMock = CreateMockStorageService();

        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "File",
            BlobName = "existing.pdf",
            ContentType = "application/pdf",
            Timestamp = DateTime.UtcNow
        };

        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { "existing.pdf", "nonexistent.pdf" } };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<List<DocumentUrlResponseDto>>>().Subject;
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().BlobName.Should().Be("existing.pdf");
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsProblem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("BulkUrls_DbException_" + Guid.NewGuid())
            .Options;

        var context = new ApplicationContext(options);
        var storageServiceMock = CreateMockStorageService();

        var handler = new GetBulkDocumentUrlsHandler(context, storageServiceMock.Object);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { "blob.pdf" } };
        var request = new GetBulkDocumentUrlsRequest(dto);

        // Dispose context to simulate database error
        await context.DisposeAsync();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
