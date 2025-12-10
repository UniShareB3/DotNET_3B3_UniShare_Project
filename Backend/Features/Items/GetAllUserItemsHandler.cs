using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Items;

public class GetAllUserItemsHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetAllUserItemsRequest, IResult>
{
    private readonly ILogger _logger= Log.ForContext<GetAllUserItemsHandler>();
    public async Task<IResult> Handle(GetAllUserItemsRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to retrieve items for user {UserId}", request.UserId);
        try
        {
            var query = dbContext.Items.Where(item => item.OwnerId == request.UserId);
            var items = await query.ProjectTo<ItemDto>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            _logger.Information("Successfully retrieved {ItemCount} items for user {UserId}", items.Count, request.UserId);
            return Results.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while retrieving items for user {UserId}", request.UserId);
            return Results.InternalServerError("An unexpected error occurred while retrieving user items.");
        }
    }
}