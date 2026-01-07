using Backend.Features.Shared.StripeService.DTO;
using MediatR;

namespace Backend.Features.Shared.StripeService.CreateCheckoutSession;

/// <summary>
/// Request to create a Stripe checkout session for a booking payment
/// </summary>
public record CreateCheckoutSessionRequest(CreateCheckoutSessionDto Dto) : IRequest<IResult>;
