using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Backend.Features.Items
{
    public class GetBookingsForItemHandler : IRequestHandler<GetBookingsForItemRequest, IResult>
    {
        private readonly ApplicationContext _dbContext;
        public GetBookingsForItemHandler(ApplicationContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IResult> Handle(GetBookingsForItemRequest request, CancellationToken cancellationToken)
        {
            var bookings = await _dbContext.Bookings
                .Where(b => b.ItemId == request.ItemId)
                .ToListAsync(cancellationToken);
            return Results.Ok(bookings);
        }
    }
}

