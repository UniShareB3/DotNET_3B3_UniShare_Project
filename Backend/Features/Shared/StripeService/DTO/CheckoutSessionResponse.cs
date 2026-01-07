namespace Backend.Features.Shared.StripeService.DTO;

/// <summary>
/// Response DTO containing the Stripe checkout session URL
/// </summary>
public record CheckoutSessionResponse(
    string Url,
    string SessionId
);
