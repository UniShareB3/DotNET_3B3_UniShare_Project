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
    public class GetBookingsForItemHandler : IRequestHandler<GetBookingsForItemRequest, IResult>
    {
        private readonly ApplicationContext _dbContext;
        private readonly IMapper _mapper;
        
        public GetBookingsForItemHandler(ApplicationContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IResult> Handle(GetBookingsForItemRequest request, CancellationToken cancellationToken)
        {
            // Check if item exists first
            var itemExists = await _dbContext.Items.AnyAsync(i => i.Id == request.ItemId, cancellationToken);
            if (!itemExists)
            {
                return Results.NotFound();
            }
            
            var bookings = await _dbContext.Bookings
                .Where(b => b.ItemId == request.ItemId)
                .ProjectTo<BookingDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            
            return Results.Ok(bookings);
        }
    }
}

