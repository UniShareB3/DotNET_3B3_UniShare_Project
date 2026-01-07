using AutoMapper;
using Backend.Data;
using Backend.Features.Universities;
using Backend.Features.Universities.DTO;
using Backend.Features.Universities.GetAllUniversities;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.Handlers.Universities;

public class GetAllUniversitiesHandlerTests
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
            .Setup(m => m.Map<UniversityDto>(It.IsAny<University>()))
            .Returns((Func<University, UniversityDto>)(src => new UniversityDto
            {
                Id = src.Id,
                Name = src.Name,
                ShortCode = src.ShortCode,
                EmailDomain = src.EmailDomain
            }));

        return mapperMock.Object;
    }

    [Fact]
    public async Task Given_UniversitiesExist_When_Handle_Then_ReturnsAllUniversities()
    {
        // Arrange
        var context = CreateInMemoryDbContext("a1b2c3d4-e5f6-7a8b-9c0d-e1f2a3a4c5d6");
        var mapper = CreateMapper();
        
        var university1 = new University
        {
            Id = Guid.Parse("bbcdefaa-cdef-abcd-efab-cdefabcdafaa"),
            Name = "University A",
            ShortCode = "UA",
            EmailDomain = "student.ua.ro",
            CreatedAt =  DateTime.UtcNow
        };
        
        var university2 = new University
        {
            Id = Guid.Parse("bbcdefaa-cdef-abcd-efab-cdefabcdaf00"),
            Name = "University B",
            ShortCode = "UB",
            EmailDomain = "student.ub.ro",
            CreatedAt =  DateTime.UtcNow
        };
        
        context.Universities.AddRange(university1, university2);
        await context.SaveChangesAsync();
        
        var handler = new GetAllUniversitiesHandler(context,mapper);
        var request = new GetAllUniversitiesRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
    
    [Fact]
    public async Task Given_NoUniversitiesExist_When_Handle_Then_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryDbContext("d6c5b4a3-2f1e-0d9c-8b7a-6f5e4d3c2b1a");
        var mapper = CreateMapper();
        
        var handler = new GetAllUniversitiesHandler(context,mapper);
        var request = new GetAllUniversitiesRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;
        var universities = valueResult.Value.Should().BeAssignableTo<IEnumerable<UniversityDto>>().Subject;

        universities.Should().BeEmpty();
    }
}