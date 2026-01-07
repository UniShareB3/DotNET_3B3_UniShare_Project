using Backend.Features.Shared.StripeService.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stripe;
using Stripe.Checkout;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.StripeService.CreateCheckoutSession;

/// <summary>
/// Handler for creating Stripe checkout sessions for booking payments
/// </summary>
public class CreateCheckoutSessionHandler(
    ApplicationContext dbContext,
    IConfiguration configuration)
    : IRequestHandler<CreateCheckoutSessionRequest, IResult>
{
    private readonly ILogger _logger = Log.ForContext<CreateCheckoutSessionHandler>();

    public async Task<IResult> Handle(CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Creating checkout session for booking {BookingId}", request.Dto.BookingId);

            // Get the environment to determine which API key to use
            var environment = configuration["Environment"] ?? "Development";
            var stripeSecretKey = environment == "Production"
                ? configuration["Stripe:Live_Secret_Key"]
                : configuration["Stripe:Test_Secret_Key"];

            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                _logger.Error("Stripe secret key not configured for environment {Environment}", environment);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Configuration error",
                    detail: "Stripe is not properly configured");
            }

            StripeConfiguration.ApiKey = stripeSecretKey;

            // Find the booking with related item and owner
            var booking = await dbContext.Bookings
                .Include(b => b.Item)
                .ThenInclude(i => i!.Owner)
                .FirstOrDefaultAsync(b => b.Id == request.Dto.BookingId, cancellationToken);

            if (booking == null)
            {
                _logger.Warning("Booking {BookingId} not found", request.Dto.BookingId);
                return Results.NotFound(new { message = "Booking not found" });
            }

            if (booking.Item == null)
            {
                _logger.Error("Booking {BookingId} has no associated item", request.Dto.BookingId);
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid booking",
                    detail: "Booking has no associated item");
            }

            if (booking.IsPaid)
            {
                _logger.Warning("Booking {BookingId} is already paid", request.Dto.BookingId);
                return Results.BadRequest(new { message = "This booking has already been paid" });
            }

            // Get the item owner
            var owner = booking.Item.Owner;
            if (owner == null || string.IsNullOrEmpty(owner.StripeAccountId))
            {
                _logger.Error("Item owner {OwnerId} does not have a Stripe account", booking.Item.OwnerId);
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Owner not configured",
                    detail: "The item owner has not set up their payment account");
            }

            var amount = (long)(booking.Item.Price * 0.4);

            // Create checkout session
            var sessionService = new SessionService();
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = booking.Item.Name,
                                Description =
                                    $"Rental from {booking.StartDate:yyyy-MM-dd} to {booking.EndDate:yyyy-MM-dd}",
                            },
                            UnitAmount = amount,
                        },
                        Quantity = 1,
                    }

                ],
                Mode = "payment",
                SuccessUrl = request.Dto.SuccessUrl,
                CancelUrl = request.Dto.CancelUrl,
                ClientReferenceId = booking.Id.ToString(),
            };

            var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.Information("Created checkout session {SessionId} for booking {BookingId}", session.Id, booking.Id);

            return Results.Ok(new CheckoutSessionResponse(session.Url, session.Id));
        }
        catch (StripeException ex)
        {
            _logger.Error(ex, "Stripe error while creating checkout session for booking {BookingId}", request.Dto.BookingId);
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Stripe error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating checkout session for booking {BookingId}", request.Dto.BookingId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred");
        }
    }
}
