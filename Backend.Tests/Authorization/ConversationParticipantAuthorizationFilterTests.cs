using System.Reflection;
using System.Security.Claims;
using Backend.Data;
using Backend.Features.Conversations.DTO;
using Backend.Features.Shared.Authorization.ConversationParticipantAuthorization;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Authorization;

public class ConversationParticipantAuthorizationFilterTests
{
    private static ApplicationContext CreateInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new ApplicationContext(options);
    }

    private static DefaultHttpContext CreateHttpContext(ApplicationContext dbContext, Guid? userId = null, bool isAdmin = false)
    {
        var httpContext = new DefaultHttpContext();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => dbContext);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        return httpContext;
    }

    private static EndpointFilterInvocationContext CreateFilterContext(
        HttpContext httpContext, 
        params object?[] arguments)
    {
        return new DefaultEndpointFilterInvocationContext(httpContext, arguments);
    }

    private static ValueTask<object?> NextDelegate(EndpointFilterInvocationContext context)
    {
        return new ValueTask<object?>(Results.Ok("Success"));
    }

    // Helper method to invoke the private filter method using reflection
    private static async Task<object?> InvokeFilter(EndpointFilterInvocationContext context)
    {
        var filterType = typeof(ConversationParticipantAuthorizationFilter);
        var method = filterType.GetMethod("ProcessAuthorizationFilter", BindingFlags.NonPublic | BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException("ProcessAuthorizationFilter method not found");
        }

        EndpointFilterDelegate nextDelegate = NextDelegate;
        var task = (ValueTask<object?>)method.Invoke(null, new object[] { context, nextDelegate })!;
        return await task;
    }

    #region Single Blob Authorization Tests

    [Fact]
    public async Task Given_ValidUserAndAccessToSingleBlob_When_ProcessAuthorizationFilter_Then_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_ValidUserAndAccessToSingleBlob_AsSender_When_ProcessAuthorizationFilter_Then_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "sender-blob.pdf";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "application/pdf"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_ValidUserAndAccessToSingleBlob_AsReceiver_When_ProcessAuthorizationFilter_Then_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "receiver-blob.png";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = userId,
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/png"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_UserWithoutAccessToSingleBlob_When_ProcessAuthorizationFilter_Then_ReturnsForbid()
    {
        // Arrange
        var unauthorizedUserId = Guid.NewGuid();
        var blobName = "private-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, unauthorizedUserId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Given_NonExistentBlob_When_ProcessAuthorizationFilter_Then_ReturnsForbid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "non-existent-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    #endregion

    #region Bulk Blob Authorization Tests

    [Fact]
    public async Task Given_ValidUserAndAccessToAllBulkBlobs_When_ProcessAuthorizationFilter_Then_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var blobName1 = "bulk-blob-1.jpg";
        var blobName2 = "bulk-blob-2.pdf";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = receiverId,
            BlobName = blobName1,
            Content = "Test message 1",
            ContentType = "image/jpeg"
        };
        var chatMessage2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = receiverId,
            BlobName = blobName2,
            Content = "Test message 2",
            ContentType = "application/pdf"
        };
        dbContext.ChatMessages.AddRange(chatMessage1, chatMessage2);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { blobName1, blobName2 } };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_UserWithAccessToSomeBulkBlobs_When_ProcessAuthorizationFilter_Then_ReturnsForbid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName1 = "accessible-blob.jpg";
        var blobName2 = "inaccessible-blob.pdf";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // User has access to first blob
        var chatMessage1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName1,
            Content = "Test message 1",
            ContentType = "image/jpeg"
        };
        
        // User does NOT have access to second blob
        var chatMessage2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName2,
            Content = "Test message 2",
            ContentType = "application/pdf"
        };
        dbContext.ChatMessages.AddRange(chatMessage1, chatMessage2);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { blobName1, blobName2 } };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Given_DuplicateBlobNamesInBulkRequest_When_ProcessAuthorizationFilter_Then_HandlesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "duplicate-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { blobName, blobName, blobName } };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task Given_UnauthenticatedUser_When_ProcessAuthorizationFilter_Then_ReturnsUnauthorized()
    {
        // Arrange
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext); // No user ID
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Given_InvalidUserIdClaim_When_ProcessAuthorizationFilter_Then_ReturnsUnauthorized()
    {
        // Arrange
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => dbContext);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        // Invalid claim value
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Given_UserIdFromSubClaim_When_ProcessAuthorizationFilter_Then_AuthorizesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => dbContext);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        // Use "sub" claim instead of NameIdentifier
        var claims = new List<Claim>
        {
            new("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_AdminUser_When_ProcessAuthorizationFilter_Then_BypassesAuthorizationCheck()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Admin user trying to access blob they don't have access to
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Test message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId, isAdmin: true);
        // Enable admin bypass
        httpContext.Items["__AdminBypassEnabled"] = true;

        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task Given_EmptyBlobNameList_When_ProcessAuthorizationFilter_Then_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = new List<string>() };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
    }

    [Fact]
    public async Task Given_NoDocumentDtoInArguments_When_ProcessAuthorizationFilter_Then_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext, userId);
        var filterContext = CreateFilterContext(httpContext); // No arguments

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>();
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public async Task Given_MessageWithNullBlobName_When_ProcessAuthorizationFilter_Then_DoesNotMatchRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "test-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = null, // Null blob name
            Content = "Text message without attachment",
            ContentType = "text/plain"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Given_MultipleMessagesWithSameBlobName_When_ProcessAuthorizationFilter_Then_AuthorizesIfUserHasAccessToAny()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "shared-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Multiple messages with the same blob name
        var chatMessage1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "First message",
            ContentType = "image/jpeg"
        };
        var chatMessage2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName,
            Content = "Second message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.AddRange(chatMessage1, chatMessage2);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_MixedSingleAndBulkDtosInArguments_When_ProcessAuthorizationFilter_Then_ValidatesAllBlobs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName1 = "single-blob.jpg";
        var blobName2 = "bulk-blob-1.pdf";
        var blobName3 = "bulk-blob-2.png";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var chatMessage1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName1,
            Content = "Message 1",
            ContentType = "image/jpeg"
        };
        var chatMessage2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName2,
            Content = "Message 2",
            ContentType = "application/pdf"
        };
        var chatMessage3 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            BlobName = blobName3,
            Content = "Message 3",
            ContentType = "image/png"
        };
        dbContext.ChatMessages.AddRange(chatMessage1, chatMessage2, chatMessage3);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var singleDto = new GetDocumentUrlDto { BlobName = blobName1 };
        var bulkDto = new GetBulkDocumentUrlsDto { BlobNames = new List<string> { blobName2, blobName3 } };
        var filterContext = CreateFilterContext(httpContext, singleDto, bulkDto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_EmptyStringBlobName_When_ProcessAuthorizationFilter_Then_ReturnsForbid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = string.Empty };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Given_BothSenderAndReceiver_When_SameUserAccessBlob_Then_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blobName = "self-message-blob.jpg";
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        // Message sent to oneself
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            ReceiverId = userId,
            BlobName = blobName,
            Content = "Self message",
            ContentType = "image/jpeg"
        };
        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = blobName };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_LargeBulkBlobList_When_ProcessAuthorizationFilter_Then_ValidatesAllCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        
        var blobNames = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var blobName = $"blob-{i}.jpg";
            blobNames.Add(blobName);
            
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = userId,
                ReceiverId = receiverId,
                BlobName = blobName,
                Content = $"Message {i}",
                ContentType = "image/jpeg"
            };
            dbContext.ChatMessages.Add(chatMessage);
        }
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetBulkDocumentUrlsDto { BlobNames = blobNames };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>();
    }

    [Fact]
    public async Task Given_WhitespaceBlobName_When_ProcessAuthorizationFilter_Then_ReturnsForbid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dbContext = CreateInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = CreateHttpContext(dbContext, userId);
        var dto = new GetDocumentUrlDto { BlobName = "   " };
        var filterContext = CreateFilterContext(httpContext, dto);

        // Act
        var result = await InvokeFilter(filterContext);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    #endregion
}

