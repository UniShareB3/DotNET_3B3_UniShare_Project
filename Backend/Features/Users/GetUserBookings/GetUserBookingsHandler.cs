using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Users.GetUserBookings;

public class GetUserBookingsHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<GetUserBookingsRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<GetUserBookingsHandler>();
    
    public async Task<IResult> Handle(GetUserBookingsRequest request, CancellationToken cancellationToken)
    {
        var bookings = await dbContext.Bookings
            .Where(b => b.BorrowerId == request.UserId)
            .Include(b => b.Item)
            .ProjectTo<BookingDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        _logger.Information("Retrieved bookings for user {UserId} from the database.", request.UserId);
        return Results.Ok(bookings);
    }
}