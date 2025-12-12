using MediatR;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger=Serilog.ILogger;

namespace Backend.Features.Items;

public class DeleteItemHandler(ApplicationContext dbContext) : IRequestHandler<DeleteItemRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<DeleteItemHandler>();
    public async Task<IResult> Handle(DeleteItemRequest request, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to delete item {ItemId}", request.Id);
        
        try
        {
            var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
            
            if (item == null)
            {
                _logger.Warning("Deletion failed: Item {ItemId} not found.", request.Id);
                return Results.NotFound();
            }

            dbContext.Items.Remove(item);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information("Item {ItemId} deleted successfully.", item.Id);
            
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while deleting item {ItemId}", request.Id);
            return Results.InternalServerError();
        }
    }
}