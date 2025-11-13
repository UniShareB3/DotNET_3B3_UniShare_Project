using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Items;

public class GetAllItemsHandler(ApplicationContext dbContext)
{
    public async Task<IResult> Handle()    
    {
        var items = await dbContext.Items
            .Include(i => i.Owner) // Include Owner pentru a accesa numele
            .Select(i => new ItemDto(
                i.Id,
                i.Name,
                i.Description, // Păstrează-l sau pune o versiune trunchiată
                i.Category.ToString(), // Convertește enum-ul la string
                i.Condition.ToString(), // Convertește enum-ul la string
                i.IsAvailable,
                i.ImageUrl, 
                i.Owner.FirstName + " " + i.Owner.LastName
            ))
            .ToListAsync();
        return Results.Ok(items);
    }
}