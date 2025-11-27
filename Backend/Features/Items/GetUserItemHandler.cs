using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Backend.Features.Items;

public class GetUserItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetUserItemRequest, IResult>
{
    public async Task<IResult> Handle(GetUserItemRequest request, CancellationToken cancellationToken)
    {
        var query =dbContext.Items.Where(item => item.OwnerId==request.UserId && item.Id==request.ItemId);
        var item=await query.
            ProjectTo<ItemDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        return item == null ? Results.NotFound() : Results.Ok(item);
    }
}