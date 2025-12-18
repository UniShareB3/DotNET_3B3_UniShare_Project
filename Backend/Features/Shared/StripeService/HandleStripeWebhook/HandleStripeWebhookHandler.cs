using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;
using Stripe;
using Stripe.Checkout;

namespace Backend.Features.Shared.StripeService.HandleStripeWebhook;

/// <summary>
/// Handler for processing Stripe webhook events
/// </summary>
public class HandleStripeWebhookHandler : IRequestHandler<HandleStripeWebhookRequest, IResult>
{
    private readonly ApplicationContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger = Log.ForContext<HandleStripeWebhookHandler>();

    public HandleStripeWebhookHandler(
        ApplicationContext dbContext,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<IResult> Handle(HandleStripeWebhookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Processing Stripe webhook");

            // Get the environment to determine which API key to use
            var environment = _configuration["Environment"] ?? "Development";
            var stripeSecretKey = environment == "Production"
                ? _configuration["Stripe:Live_Secret_Key"]
                : _configuration["Stripe:Test_Secret_Key"];

            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                _logger.Error("Stripe secret key not configured");
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Configuration error",
                    detail: "Stripe is not properly configured");
            }

            StripeConfiguration.ApiKey = stripeSecretKey;

            // Verify the webhook signature
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            Event stripeEvent;

            try
            {
                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        request.Json,
                        request.SignatureHeader,
                        webhookSecret
                    );
                }
                else
                {
                    // If no webhook secret is configured, parse the event directly (not recommended for production)
                    _logger.Warning("No webhook secret configured. Skipping signature verification.");
                    stripeEvent = EventUtility.ParseEvent(request.Json);
                }
            }
            catch (StripeException ex)
            {
                _logger.Error(ex, "Failed to verify webhook signature");
                return Results.BadRequest(new { error = "Invalid signature" });
            }

            _logger.Information("Processing webhook event type: {EventType}", stripeEvent.Type);

            // Handle different event types
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent, cancellationToken);
                    break;

                case "account.updated":
                    await HandleAccountUpdated(stripeEvent, cancellationToken);
                    break;

                case "payment_intent.succeeded":
                    _logger.Information("Payment intent succeeded");
                    break;

                default:
                    _logger.Information("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Results.Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing webhook");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Webhook processing error",
                detail: "An error occurred while processing the webhook");
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.Warning("Checkout session object is null");
            return;
        }

        _logger.Information("Checkout session completed: {SessionId}", session.Id);

        // Extract booking ID from metadata
        
        
        if (session.ClientReferenceId == null)
        {
            _logger.Warning("Checkout session {SessionId} has no booking_id in metadata", session.Id);
            return;
        }

        var clientReferenceId = session.ClientReferenceId;
        
        if (!Guid.TryParse(clientReferenceId, out Guid clientGuid))
        {
            _logger.Error("Invalid ClientReferenceId format: {ClientReferenceId}", clientReferenceId);
            return;
        }

        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == clientGuid);

        if (booking == null)
        {
            _logger.Error("Could not find booking for ClientReferenceId: {ClientReferenceId}", clientReferenceId);
            return;
        }
        
        // Mark the booking as paid
        booking.IsPaid = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information("Marked booking {BookingId} as paid", booking.Id);
    }

    private async Task HandleAccountUpdated(Event stripeEvent, CancellationToken cancellationToken)
    {
        var account = stripeEvent.Data.Object as Account;
        if (account == null)
        {
            _logger.Warning("Account object is null");
            return;
        }

        _logger.Information("Stripe account updated: {AccountId}, ChargesEnabled: {ChargesEnabled}, DetailsSubmitted: {DetailsSubmitted}",
            account.Id, account.ChargesEnabled, account.DetailsSubmitted);

        // Find user with this Stripe account
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.StripeAccountId == account.Id, cancellationToken);

        if (user == null)
        {
            _logger.Warning("No user found with Stripe account {AccountId}", account.Id);
            return;
        }

        // If the account is fully onboarded and can accept charges, grant Seller role
        if (account.ChargesEnabled && account.DetailsSubmitted)
        {
            var hasSellerRole = await _userManager.IsInRoleAsync(user, "Seller");
            if (!hasSellerRole)
            {
                await _userManager.AddToRoleAsync(user, "Seller");
                _logger.Information("Granted Seller role to user {UserId} with Stripe account {AccountId}",
                    user.Id, account.Id);
            }
        }
        else
        {
            // If account is not fully enabled, remove Seller role if they have it
            var hasSellerRole = await _userManager.IsInRoleAsync(user, "Seller");
            if (hasSellerRole)
            {
                await _userManager.RemoveFromRoleAsync(user, "Seller");
                _logger.Information("Removed Seller role from user {UserId} with Stripe account {AccountId}",
                    user.Id, account.Id);
            }
        }
    }
}
