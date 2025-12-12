using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger=Serilog.ILogger;

namespace Backend.Features.Items;

public class GetAllItemsHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetAllItemsRequest, IResult>
{
    private readonly ILogger _logger= Log.ForContext<GetAllItemsHandler>();
    public async Task<IResult> Handle(GetAllItemsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to retrieve all items.");
        try
        {
            var query = dbContext.Items;
            var items = await query.ProjectTo<ItemDto>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            _logger.Information("Successfully retrieved {ItemCount} items.", items.Count);
            return Results.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while retrieving all items");
            return Results.Problem("An unexpected error occurred while retrieving items.");
        }
    }
}