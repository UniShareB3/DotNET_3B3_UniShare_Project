using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Items.GetItem;

public class GetItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetItemRequest, IResult>
{
    private readonly ILogger _logger= Log.ForContext<GetItemHandler>();
    public async Task<IResult> Handle(GetItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to retrieve item {ItemId}", request.Id);
        try
        {
            var query = dbContext.Items.Where(i => i.Id == request.Id);
            var itemDto = await query.ProjectTo<ItemDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
            if (itemDto == null)
            {
                _logger.Warning("Retrieval failed: Item {ItemId} not found.", request.Id);
                return Results.NotFound();
            }
            _logger.Information("Successfully retrieved item {ItemId}", request.Id);
            return Results.Ok(itemDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while retrieving item {ItemId}", request.Id);
            return Results.Problem("An unexpected error occurred while retrieving the item.");
        }
    }
}