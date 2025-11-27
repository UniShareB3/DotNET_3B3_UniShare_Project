using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Backend.Features.Items;

public class GetAllUserItemsHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetAllUserItemsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllUserItemsRequest request, CancellationToken cancellationToken)
    {
        var query=dbContext.Items.Where(item=>item.OwnerId==request.UserId);
        var items =await query.
            ProjectTo<ItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }
}