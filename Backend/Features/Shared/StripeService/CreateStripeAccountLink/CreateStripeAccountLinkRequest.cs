
using Backend.Features.Shared.StripeService.DTO;
using MediatR;

namespace Backend.Features.Shared.StripeService.CreateStripeAccountLink;

/// <summary>
/// Request to create a Stripe Connect account link for user onboarding
/// </summary>
public record CreateStripeAccountLinkRequest(CreateStripeAccountLinkDto Dto) : IRequest<IResult>;
