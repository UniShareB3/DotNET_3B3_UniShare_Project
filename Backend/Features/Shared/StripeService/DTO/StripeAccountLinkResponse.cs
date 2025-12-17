namespace Backend.Features.Shared.Stripe.DTO;

/// <summary>
/// Response DTO containing the Stripe account onboarding URL
/// </summary>
public record StripeAccountLinkResponse(
    string Url
);
