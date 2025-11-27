using MediatR;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Items;

public class DeleteItemHandler(ApplicationContext dbContext) : IRequestHandler<DeleteItemRequest, IResult>
{
    public async Task<IResult> Handle(DeleteItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (item == null)
        {
            return Results.NotFound();
        }

        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}