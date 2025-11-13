using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Items;

public class GetUserItemHandler(ApplicationContext dbContext)
{
    public async Task<IResult> Handle(GetUserItemRequest request)
    {
        var item = await dbContext.Items
            .Include(i => i.Owner)
            .Where(i => i.Id == request.ItemId && i.OwnerId == request.UserId)
            .Select(i=>new ItemDto(
                i.Id,
                i.Name,
                i.Description,
                i.Category.ToString(),
                i.Condition.ToString(),
                i.IsAvailable,
                i.ImageUrl,
                i.Owner.FirstName + " " + i.Owner.LastName
            ))
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(item);
    }
}