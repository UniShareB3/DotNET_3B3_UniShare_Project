using Backend.Features.Shared.Stripe.DTO;
using MediatR;

namespace Backend.Features.Shared.Stripe;

/// <summary>
/// Request to create a Stripe Connect account link for user onboarding
/// </summary>
public record CreateStripeAccountLinkRequest(CreateStripeAccountLinkDto Dto) : IRequest<IResult>;
