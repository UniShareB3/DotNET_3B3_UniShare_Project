namespace Backend.Features.Shared.Stripe.DTO;

/// <summary>
/// DTO for creating a Stripe Connect account onboarding link
/// </summary>
public record CreateStripeAccountLinkDto(
    Guid UserId,
    string ReturnUrl,
    string RefreshUrl
);
