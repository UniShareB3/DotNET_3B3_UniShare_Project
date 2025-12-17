namespace Backend.Features.Shared.Stripe.DTO;

/// <summary>
/// Response DTO containing the Stripe checkout session URL
/// </summary>
public record CheckoutSessionResponse(
    string Url,
    string SessionId
);
