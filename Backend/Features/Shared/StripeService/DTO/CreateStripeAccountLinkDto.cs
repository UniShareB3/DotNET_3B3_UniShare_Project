namespace Backend.Features.Shared.StripeService.DTO;

/// <summary>
/// DTO for creating a Stripe Connect account onboarding link
/// </summary>
public abstract record CreateStripeAccountLinkDto(
    Guid UserId,
    string ReturnUrl,
    string RefreshUrl
);
