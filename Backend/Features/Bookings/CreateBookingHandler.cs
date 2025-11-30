using AutoMapper;
using Backend.Data;
using Backend.Features.Booking.DTO;
using Backend.Validators;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Backend.Persistence;
using MediatR;

namespace Backend.Features.Booking;

public class CreateBookingHandler(ApplicationContext dbContext, IMapper mapper,  ILogger<CreateBookingHandler> logger) : IRequestHandler<CreateBookingRequest, IResult>
{
    public async Task<IResult> Handle(CreateBookingRequest? request, CancellationToken cancellationToken)
    {
       // Nota Bianca: nu inteleg de ce trebuie folosit Data.Booking desi am important Backend.Data
        var booking = mapper.Map<Data.Booking>(request.Booking);
        
        if (booking.Id == Guid.Empty)
            booking.Id = Guid.NewGuid();

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking created");
        return Results.Created($"/bookings/{booking.Id}", booking);
    }
}