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

public class CreateBookingHandler(ApplicationContext dbContext, IMapper mapper, CreateBookingValidator validator, ILogger<CreateBookingHandler> logger) : IRequestHandler<CreateBookingRequest, IResult>
{
    public async Task<IResult> Handle(CreateBookingRequest? request, CancellationToken cancellationToken)
    {
        if (request is null){
            logger.LogError("Booking request is null");
            return Results.BadRequest(new { Error = "Booking payload is required." });
            }
        
        ValidationResult validationResult;
        try
        {
            validationResult = await validator.ValidateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed due to an internal error.");
            return Results.BadRequest(new { Error = "Validation failed. Internal error.", Details = ex.Message });
        }

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            
            logger.LogError("Validation failed: {Errors}", string.Join("; ", errors));
            return Results.BadRequest(new { Errors = errors });
        }
        
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