using AutoMapper;
using Backend.Persistence;
using Backend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using ItemDto = Backend.Features.Items.DTO.ItemDto;

namespace Backend.Features.Items.PostItem;


public class PostItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<PostItemRequest, IResult>
{
    private  readonly ILogger _logger= Log.ForContext<PostItemHandler>();
    public async Task<IResult> Handle(PostItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to create a new item with name {ItemName}", request.Item.Name);
        try
        {
            var owner = await dbContext.Users.Where(o => o.Id == request.Item.OwnerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (owner == null)
            {
                _logger.Warning("Creation failed: Owner with ID {OwnerId} not found.", request.Item.OwnerId);
                return Results.NotFound(new
                {
                    Error = "Validation Failed",
                    Details = $"User with ID '{request.Item.OwnerId}' not found. Cannot assign ownership."
                });
            }

            var item = mapper.Map<Item>(request.Item);

            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Item {ItemId} with name {ItemName} created successfully by owner {OwnerId}.", 
                item.Id, item.Name, item.OwnerId);
            
            // Load the owner to map to DTO
            await dbContext.Entry(item).Reference(i => i.Owner).LoadAsync(cancellationToken);
            var itemDto = mapper.Map<ItemDto>(item);
                

            return Results.Created($"/items/{item.Id}", itemDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating item with name {ItemName}", request.Item.Name);
            return Results.InternalServerError("An unexpected error occurred while creating the item.");
        }
    }
}