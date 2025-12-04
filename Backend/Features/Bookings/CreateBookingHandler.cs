using AutoMapper;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Bookings;

public class CreateBookingHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<CreateBookingRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<CreateBookingHandler>();

    public async Task<IResult> Handle(CreateBookingRequest? request, CancellationToken cancellationToken)
    {
        _logger.Information("Creating booking for item {ItemId}", request.Booking.ItemId);
        
        var booking = mapper.Map<Data.Booking>(request.Booking);
        
        if (booking.Id == Guid.Empty)
        {
            booking.Id = Guid.NewGuid();
        }

        try
        {
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Booking {BookingId} created successfully for item {ItemId}", 
                booking.Id, booking.ItemId);
                
            return Results.Created($"/bookings/{booking.Id}", booking);
        }
        catch (DbUpdateException ex)
        {
            _logger.Error(ex, "Database error while creating booking for item {ItemId}", request.Booking.ItemId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating booking for item {ItemId}", request.Booking.ItemId);
            throw;
        }
    }
}