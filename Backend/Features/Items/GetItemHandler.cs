using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Items.DTO;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Backend.Features.Items;

public class GetItemHandler(ApplicationContext dbContext,IMapper mapper) : IRequestHandler<GetItemRequest, IResult>
{
    public async Task<IResult> Handle(GetItemRequest request, CancellationToken cancellationToken)
    {
        var query=dbContext.Items.Where(i => i.Id == request.Id);
        var item =await query.
            ProjectTo<ItemDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return item == null ? Results.NotFound() : Results.Ok(item);
    }
}