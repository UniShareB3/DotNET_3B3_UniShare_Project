using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Backend.Features.Items;

public class GetAllItemsHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetAllItemsRequest, IResult>
{
    public async Task<IResult> Handle(GetAllItemsRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Items;
        var items=await query.
            ProjectTo<ItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }
}