using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Items;

public class GetAllUserItemsHandler(ApplicationContext dbContext)
{
    public async Task<IResult> Handle(GetAllUserItemsRequest request)    
    {
        var items = await dbContext.Items
            .Include(i => i.Owner)
            .Where(item => item.OwnerId == request.UserId)
            .Select(i=> new ItemDto(
                i.Id,
                i.Name,
                i.Description,
                i.Category.ToString(),
                i.Condition.ToString(),
                i.IsAvailable,
                i.ImageUrl,
                i.Owner.FirstName + " " + i.Owner.LastName
            )).ToListAsync();
        return Results.Ok(items);
    }
}