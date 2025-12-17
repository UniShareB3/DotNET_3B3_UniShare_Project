using Backend.Data;
using Backend.Features.Shared.Stripe.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stripe;
using ILogger = Serilog.ILogger;

namespace Backend.Features.Shared.Stripe;

/// <summary>
/// Handler for creating Stripe Connect account onboarding links
/// </summary>
public class CreateStripeAccountLinkHandler : IRequestHandler<CreateStripeAccountLinkRequest, IResult>
{
    private readonly ApplicationContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger = Log.ForContext<CreateStripeAccountLinkHandler>();

    public CreateStripeAccountLinkHandler(
        ApplicationContext dbContext,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<IResult> Handle(CreateStripeAccountLinkRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Creating Stripe account link for user {UserId}", request.Dto.UserId);

            // Get the environment to determine which API key to use
            var environment = _configuration["Environment"] ?? "Development";
            var stripeSecretKey = environment == "Production"
                ? _configuration["Stripe:Live_Secret_Key"]
                : _configuration["Stripe:Test_Secret_Key"];

            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                _logger.Error("Stripe secret key not configured for environment {Environment}", environment);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Configuration error",
                    detail: "Stripe is not properly configured");
            }

            StripeConfiguration.ApiKey = stripeSecretKey;

            // Find the user
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == request.Dto.UserId, cancellationToken);    
            if (user == null)
            {
                _logger.Warning("User {UserId} not found", request.Dto.UserId);
                return Results.NotFound(new { message = "User not found" });
            }

            // Check if user already has a Stripe account
            string accountId;
            if (string.IsNullOrEmpty(user.StripeAccountId))
            {
                // Create a new Stripe Connect account
                var accountService = new AccountService();
                var accountOptions = new AccountCreateOptions
                {
                    Type = "express",
                    Email = user.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                    },
                    BusinessType = "individual",
                };

                var account = await accountService.CreateAsync(accountOptions, cancellationToken: cancellationToken);
                accountId = account.Id;

                // Save the Stripe account ID
                user.StripeAccountId = accountId;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.Information("Created new Stripe account {AccountId} for user {UserId}", accountId, user.Id);
            }
            else
            {
                accountId = user.StripeAccountId;
                _logger.Information("User {UserId} already has Stripe account {AccountId}", user.Id, accountId);
            }

            // Create an account link for onboarding
            var accountLinkService = new AccountLinkService();
            var accountLinkOptions = new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = request.Dto.RefreshUrl,
                ReturnUrl = request.Dto.ReturnUrl,
                Type = "account_onboarding",
            };

            var accountLink = await accountLinkService.CreateAsync(accountLinkOptions, cancellationToken: cancellationToken);

            _logger.Information("Created account link for user {UserId}", user.Id);

            return Results.Ok(new StripeAccountLinkResponse(accountLink.Url));
        }
        catch (StripeException ex)
        {
            _logger.Error(ex, "Stripe error while creating account link for user {UserId}", request.Dto.UserId);
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Stripe error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error while creating account link for user {UserId}", request.Dto.UserId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred");
        }
    }
}
