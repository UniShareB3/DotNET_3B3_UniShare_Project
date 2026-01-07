using AutoMapper;
using Backend.Data;
using Backend.Features.Universities.DTO;
using Backend.Features.Universities.PostUniversities;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Universities;

public class PostUniversitiesHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    private static IMapper CreateMapper()
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<University>(It.IsAny<PostUniversityDto>()))
            .Returns((Func<PostUniversityDto, University>)(src => new University
            {
                Name = src.Name,
                ShortCode = src.ShortCode,
                EmailDomain = src.EmailDomain
            }));

        return mapperMock.Object;
    }
    
    [Fact]
    public async Task Given_ValidUniversityDto_When_Handle_Then_AddsUniversityToDatabase()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7888-9c0d-e1f2a3a4c5d6");
        var mapper = CreateMapper();
        var handler = new PostUniversitiesHandler(context, mapper);
        var dto = new PostUniversityDto
        {
            Name = "Test University",
            ShortCode = "TU",
            EmailDomain = "student.tu.ro"
        };
        var request = new PostUniversitiesRequest(dto);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        var addedUniversity = await context.Universities.FirstOrDefaultAsync(u => u.Name == "Test University");
        addedUniversity.Should().NotBeNull();
        addedUniversity!.ShortCode.Should().Be("TU");
        addedUniversity.EmailDomain.Should().Be("student.tu.ro");
    }
}