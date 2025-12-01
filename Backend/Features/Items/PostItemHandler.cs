using AutoMapper;
using Backend.Persistence;
using Backend.Data;
using Backend.Features.Items.DTO;
using Backend.Features.Items.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Items;

public class PostItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<PostItemRequest, IResult>
{
    public async Task<IResult> Handle(PostItemRequest request, CancellationToken cancellationToken)
    {
        var owner = await dbContext.Users.Where(o=>o.Id==request.Item.OwnerId).FirstOrDefaultAsync(cancellationToken);

        if (owner == null)
        {
            return Results.NotFound(new 
            {
                Error = "Validation Failed", 
                Details = $"User with ID '{request.Item.OwnerId}' not found. Cannot assign ownership."
            });
        }
        
        var item = mapper.Map<Item>(request.Item);

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/items/{item.Id}", mapper.Map<ItemDto>(item));
    }
}