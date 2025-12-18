using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Bookings.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Backend.Features.Items
{
    public class GetBookingsForItemHandler(ApplicationContext dbContext, IMapper mapper)
        : IRequestHandler<GetBookingsForItemRequest, IResult>
    {
        public async Task<IResult> Handle(GetBookingsForItemRequest request, CancellationToken cancellationToken)
        {
            // Check if item exists first
            var itemExists = await dbContext.Items.AnyAsync(i => i.Id == request.ItemId, cancellationToken);
            if (!itemExists)
            {
                return Results.NotFound();
            }
            
            var bookings = await dbContext.Bookings
                .Where(b => b.ItemId == request.ItemId)
                .ProjectTo<BookingDto>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            
            return Results.Ok(bookings);
        }
    }
}

