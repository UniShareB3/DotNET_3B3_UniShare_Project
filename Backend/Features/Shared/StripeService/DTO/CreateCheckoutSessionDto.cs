namespace Backend.Features.Shared.Stripe.DTO;

/// <summary>
/// DTO for creating a Stripe checkout session for a booking payment
/// </summary>
public record CreateCheckoutSessionDto(
    Guid BookingId,
    string SuccessUrl,
    string CancelUrl
);
