﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Features.Bookings.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Bookings;

public class GetBookingHandler(ApplicationContext dbContext, IMapper mapper, ILogger<GetBookingHandler> logger) : IRequestHandler<GetBookingRequest, IResult>
{
    public async Task<IResult> Handle(GetBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .Where(b => b.Id == request.BookingId)
            .Include(b => b.Item)
            .ProjectTo<BookingDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (booking == null)
        {
            logger.LogError("Booking with ID {BookingId} not found.", request.BookingId);
            return Results.NotFound();
        }
        
        logger.LogInformation("Booking with ID {BookingId} was found.", request.BookingId);
        return Results.Ok(booking);
    }
}