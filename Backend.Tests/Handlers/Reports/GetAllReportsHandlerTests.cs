using AutoMapper;
using Backend.Data;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.GetAllReports;
using Backend.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Handlers.Reports;

public class GetAllReportsHandlerTests
{
    private static ApplicationContext CreateInMemoryDbContext(string guid )
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: guid)
            .Options;

        var context = new ApplicationContext(options);
        return context;
    }
    
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Report, ReportDto>();
        }, new LoggerFactory());

        return config.CreateMapper();
    }

    [Fact]
    public async Task Given_ReportsExist_When_Handle_Then_ReturnsOkWithAllReports()
    {
        // Arrange
        var context = CreateInMemoryDbContext("get-all-reports-handler-" + Guid.NewGuid().ToString());
        var mapper = CreateMapper();
        
        List<Report> reports = new List<Report>
        {
            new Report { Id = Guid.NewGuid(), Description = "Description 1" },
            new Report { Id = Guid.NewGuid(), Description = "Description 2" }
        };
        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();
        GetAllReportsHandler handler = new GetAllReportsHandler(context, mapper);

        // Act
        var result = await handler.Handle(new GetAllReportsRequest(), CancellationToken.None) as IStatusCodeHttpResult;
        
        // Assert
        Assert.NotNull(result);
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}