using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Items;

public class GetUserItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetUserItemRequest, IResult>
{
    private readonly ILogger _logger= Log.ForContext<GetUserItemHandler>();
    public async Task<IResult> Handle(GetUserItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to retrieve item {ItemId} for user {UserId}", request.ItemId, request.UserId);
        try
        {
            var query = dbContext.Items.Where(item => item.OwnerId == request.UserId && item.Id == request.ItemId);
            var item = await query.ProjectTo<ItemDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
            if (item == null)
            {
                _logger.Warning("Retrieval failed: Item {ItemId} for user {UserId} not found.", request.ItemId, request.UserId);
                return Results.NotFound();
            }
            _logger.Information("Successfully retrieved item {ItemId} for user {UserId}", request.ItemId, request.UserId);
            return Results.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while retrieving item {ItemId} for user {UserId}", request.ItemId, request.UserId);
            return Results.Problem("An unexpected error occurred while retrieving the user item.");
        }
    }
}