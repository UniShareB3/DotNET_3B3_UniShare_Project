using Backend.Features.Shared.Stripe.DTO;
using MediatR;

namespace Backend.Features.Shared.Stripe;

/// <summary>
/// Request to create a Stripe checkout session for a booking payment
/// </summary>
public record CreateCheckoutSessionRequest(CreateCheckoutSessionDto Dto) : IRequest<IResult>;
