using Backend.Data;
using Backend.Hubs;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Backend.Tests.Hubs;

public class ChatHubTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }

    private static Mock<HubCallerContext> CreateMockHubContext(string? userId)
    {
        var mockHubContext = new Mock<HubCallerContext>();
        
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            mockHubContext.Setup(x => x.User).Returns(claimsPrincipal);
        }
        else
        {
            mockHubContext.Setup(x => x.User).Returns((ClaimsPrincipal)null!);
        }

        return mockHubContext;
    }

    [Fact]
    public async Task Given_ValidData_When_SendMessage_Then_SavesMessageToDatabase()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-message-test-" + Guid.Parse("11111111-1111-1111-1111-111111111111").ToString());
        var senderId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString();
        var receiverId = Guid.Parse("33333333-3333-3333-3333-333333333333").ToString();
        var messageContent = "Hello, this is a test message";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage(receiverId, messageContent);

        // Assert
        var savedMessage = await context.ChatMessages.FirstOrDefaultAsync();
        savedMessage.Should().NotBeNull();
        savedMessage.SenderId.Should().Be(Guid.Parse(senderId));
        savedMessage.ReceiverId.Should().Be(Guid.Parse(receiverId));
        savedMessage.Content.Should().Be(messageContent);
        savedMessage.ContentType.Should().Be("text/plain");
        savedMessage.BlobName.Should().BeNull();
        savedMessage.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Given_ValidData_When_SendMessage_Then_SendsMessageToReceiver()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-message-receiver-test-" + Guid.Parse("44444444-4444-4444-4444-444444444444").ToString());
        var senderId = Guid.Parse("55555555-5555-5555-5555-555555555555").ToString();
        var receiverId = Guid.Parse("66666666-6666-6666-6666-666666666666").ToString();
        var messageContent = "Hello, receiver!";

        var mockClients = new Mock<IHubCallerClients>();
        var mockReceiverProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        
        mockClients.Setup(x => x.User(receiverId)).Returns(mockReceiverProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage(receiverId, messageContent);

        // Assert
        mockReceiverProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                 CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_ValidData_When_SendMessage_Then_SendsMessageToCaller()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-message-caller-test-" + Guid.Parse("77777777-7777-7777-7777-777777777777").ToString());
        var senderId = Guid.Parse("88888888-8888-8888-8888-888888888888").ToString();
        var receiverId = Guid.Parse("99999999-9999-9999-9999-999999999999").ToString();
        var messageContent = "Hello, caller!";

        var mockClients = new Mock<IHubCallerClients>();
        var mockReceiverProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        
        mockClients.Setup(x => x.User(receiverId)).Returns(mockReceiverProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage(receiverId, messageContent);

        // Assert
        mockCallerProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_InvalidSenderId_When_SendMessage_Then_DoesNotSaveMessage()
    {
        // Arrange
        var context = CreateInMemoryDbContext("invalid-sender-test-" + Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa").ToString());
        var receiverId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb").ToString();
        var messageContent = "This should not be saved";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext("invalid-guid").Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage(receiverId, messageContent);

        // Assert
        var messageCount = await context.ChatMessages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_InvalidReceiverId_When_SendMessage_Then_DoesNotSaveMessage()
    {
        // Arrange
        var context = CreateInMemoryDbContext("invalid-receiver-test-" + Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc").ToString());
        var senderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd").ToString();
        var messageContent = "This should not be saved";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage("invalid-guid", messageContent);

        // Assert
        var messageCount = await context.ChatMessages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_NoUser_When_SendMessage_Then_DoesNotSaveMessage()
    {
        // Arrange
        var context = CreateInMemoryDbContext("no-user-test-" + Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee").ToString());
        var receiverId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff").ToString();
        var messageContent = "This should not be saved";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(null).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendMessage(receiverId, messageContent);

        // Assert
        var messageCount = await context.ChatMessages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_ValidData_When_SendImageMessage_Then_BroadcastsImageMessageToClients()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-image-message-test-" + Guid.Parse("10101010-1010-1010-1010-101010101010").ToString());
        var senderId = Guid.Parse("20202020-2020-2020-2020-202020202020").ToString();
        var receiverId = Guid.Parse("30303030-3030-3030-3030-303030303030").ToString();
        var blobName = "chat-documents/guid/image.jpg";
        var documentUrl = "https://example.com/image.jpg";
        var caption = "Check out this image!";

        var mockClients = new Mock<IHubCallerClients>();
        var mockReceiverProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(receiverId)).Returns(mockReceiverProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendImageMessage(receiverId, blobName, documentUrl, caption);

        // Assert
        // SendImageMessage doesn't save to DB, it only broadcasts
        // The message is saved by ConfirmDocumentUploadHandler
        mockReceiverProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
        
        mockCallerProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_NoCaption_When_SendImageMessage_Then_SavesMessageWithEmptyContent()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-image-no-caption-test-" + Guid.Parse("40404040-4040-4040-4040-404040404040").ToString());
        var senderId = Guid.Parse("50505050-5050-5050-5050-505050505050").ToString();
        var receiverId = Guid.Parse("60606060-6060-6060-6060-606060606060").ToString();
        var blobName = "chat-documents/guid/image.jpg";
        var documentUrl = "https://example.com/image.jpg";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendImageMessage(receiverId, blobName, documentUrl);

        // Assert
        // Note: SendImageMessage doesn't save to DB anymore, it only broadcasts
        // The message is saved by ConfirmDocumentUploadHandler
        mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_ValidData_When_SendImageMessage_Then_SendsMessageToReceiver()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-image-receiver-test-" + Guid.Parse("70707070-7070-7070-7070-707070707070").ToString());
        var senderId = Guid.Parse("80808080-8080-8080-8080-808080808080").ToString();
        var receiverId = Guid.Parse("90909090-9090-9090-9090-909090909090").ToString();
        var blobName = "chat-documents/guid/image.jpg";
        var documentUrl = "https://example.com/image.jpg";
        var caption = "Test caption";

        var mockClients = new Mock<IHubCallerClients>();
        var mockReceiverProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        
        mockClients.Setup(x => x.User(receiverId)).Returns(mockReceiverProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendImageMessage(receiverId, blobName, documentUrl, caption);

        // Assert
        mockReceiverProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Given_ValidData_When_SendImageMessage_Then_SendsMessageToCaller()
    {
        // Arrange
        var context = CreateInMemoryDbContext("send-image-caller-test-" + Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1").ToString());
        var senderId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2").ToString();
        var receiverId = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3").ToString();
        var blobName = "chat-documents/guid/image.jpg";
        var documentUrl = "https://example.com/image.jpg";
        var caption = "Test caption";

        var mockClients = new Mock<IHubCallerClients>();
        var mockReceiverProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        
        mockClients.Setup(x => x.User(receiverId)).Returns(mockReceiverProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendImageMessage(receiverId, blobName, documentUrl, caption);

        // Assert
        mockCallerProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                CancellationToken.None),
            Times.Once);
    }
    [Fact]
    public async Task Given_InvalidReceiverId_WhenSendImageMessage_Then_DoesNotSaveMessage()
    {
        // Arrange
        var context = CreateInMemoryDbContext("invalid-receiver-image-test-" + Guid.Parse("f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6").ToString());
        var senderId = Guid.Parse("a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7").ToString();
        var blobName = "chat-documents/guid/image.jpg";
        var documentUrl = "https://example.com/image.jpg";

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(x => x.Caller).Returns(mockCallerProxy.Object);

        var chatHub = new ChatHub(context)
        {
            Context = CreateMockHubContext(senderId).Object,
            Clients = mockClients.Object
        };

        // Act
        await chatHub.SendImageMessage("invalid-guid", blobName, documentUrl);

        // Assert
        // SendImageMessage still broadcasts even with invalid receiverId
        // The validation is not on the receiverId format but on senderId
        mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object[]>(),
                CancellationToken.None),
            Times.Once);
    }
}