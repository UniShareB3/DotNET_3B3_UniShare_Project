using Backend.Features.Conversations.DTO;
using Backend.Features.Conversations.GenerateUploadUrl;
using Backend.Services.AzureStorage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Backend.Tests.Handlers.Conversations;

public class GenerateUploadUrlHandlerTests
{
    private static Mock<IAzureStorageService> CreateMockStorageService()
    {
        var mock = new Mock<IAzureStorageService>();
        mock.Setup(s => s.GenerateUploadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns((string blobName, TimeSpan _) => $"https://storage.blob.core.windows.net/container/{blobName}?sas=uploadtoken");
        return mock;
    }

    [Fact]
    public async Task Handle_ValidFileName_ReturnsUploadUrl()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "document.pdf", ContentType = "application/pdf" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult.Value!.UploadUrl.Should().NotBeNullOrEmpty();
        okResult.Value.BlobName.Should().EndWith(".pdf");
        okResult.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_FileWithExtension_PreservesExtension()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "image.jpg", ContentType = "image/jpeg" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult.Value!.BlobName.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task Handle_FileWithUppercaseExtension_ConvertsToLowercase()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "image.PNG", ContentType = "image/png" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult.Value!.BlobName.Should().EndWith(".png");
    }

    [Fact]
    public async Task Handle_FileWithoutExtension_GeneratesValidBlobName()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "filenoext", ContentType = "application/octet-stream" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult.Value!.BlobName.Should().NotBeNullOrEmpty();
        okResult.Value.BlobName.Should().NotContain(".");
    }

    [Fact]
    public async Task Handle_GeneratesUniqueBlobName()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "document.pdf", ContentType = "application/pdf" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result1 = await handler.Handle(request, CancellationToken.None);
        var result2 = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult1 = result1.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        var okResult2 = result2.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult1.Value!.BlobName.Should().NotBe(okResult2.Value!.BlobName);
    }

    [Fact]
    public async Task Handle_BlobNameIsValidGuid()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "document.pdf", ContentType = "application/pdf" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        var blobNameWithoutExtension = okResult.Value!.BlobName.Replace(".pdf", "");
        Guid.TryParse(blobNameWithoutExtension, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StorageServiceCalled_WithCorrectExpiryTime()
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "document.pdf", ContentType = "application/pdf" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        storageServiceMock.Verify(s => s.GenerateUploadSasUrl(
            It.Is<string>(b => b.EndsWith(".pdf")),
            TimeSpan.FromMinutes(15)), Times.Once);
    }

    [Fact]
    public async Task Handle_StorageServiceException_ReturnsProblem()
    {
        // Arrange
        var storageServiceMock = new Mock<IAzureStorageService>();
        storageServiceMock.Setup(s => s.GenerateUploadSasUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Throws(new Exception("Storage service error"));

        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = "document.pdf", ContentType = "application/pdf" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData("file.pdf", ".pdf")]
    [InlineData("image.JPG", ".jpg")]
    [InlineData("document.DOCX", ".docx")]
    [InlineData("archive.tar.gz", ".gz")]
    public async Task Handle_VariousExtensions_HandlesCorrectly(string fileName, string expectedExtension)
    {
        // Arrange
        var storageServiceMock = CreateMockStorageService();
        var handler = new GenerateUploadUrlHandler(storageServiceMock.Object);
        var dto = new GenerateUploadUrlDto { FileName = fileName, ContentType = "application/octet-stream" };
        var request = new GenerateUploadUrlRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<Ok<GenerateUploadUrlResponse>>().Subject;
        okResult.Value!.BlobName.Should().EndWith(expectedExtension);
    }
}
