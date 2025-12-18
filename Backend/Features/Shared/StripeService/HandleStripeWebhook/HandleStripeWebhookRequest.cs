using MediatR;

namespace Backend.Features.Shared.StripeService.HandleStripeWebhook;

/// <summary>
/// Request to handle Stripe webhook events
/// </summary>
public record HandleStripeWebhookRequest(string Json, string SignatureHeader) : IRequest<IResult>;
