namespace Backend.Features.Shared.StripeService.DTO;

/// <summary>
/// DTO for creating a Stripe checkout session for a booking payment
/// </summary>
public abstract record CreateCheckoutSessionDto(
    Guid BookingId,
    string SuccessUrl,
    string CancelUrl
);
