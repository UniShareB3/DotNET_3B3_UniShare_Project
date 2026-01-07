namespace Backend.Features.Shared.StripeService.DTO;

/// <summary>
/// Response DTO containing the Stripe account onboarding URL
/// </summary>
public record StripeAccountLinkResponse(
    string Url
);
