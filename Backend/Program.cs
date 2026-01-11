using System.Security.Claims;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Backend.Validators;
using Backend.Data;
using Backend.Features.Bookings.CreateBooking;
using Backend.Features.Bookings.DeleteBooking;
using Backend.Features.Bookings.DTO;
using Backend.Features.Bookings.GetAllBookings;
using Backend.Features.Bookings.GetBooking;
using Backend.Features.Bookings.UpdateBooking;
using Backend.Features.Items.DeleteItem;
using Backend.Features.Items.GetAllItems;
using Backend.Features.Items.GetAllUserItems;
using Backend.Features.Items.GetBookingForItem;
using Backend.Features.Items.GetItem;
using Backend.Features.Items.GetUserItem;
using Backend.Features.Items.PostItem;
using Backend.Features.ModeratorAssignment.CreateModeratorAssignment;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.GetAllModeratorAssignments;
using Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;
using Backend.Features.Shared.Authorization;
using Backend.Features.Users.GetAdmins;
using Backend.Features.Users.GetModerators;
using MediatR;
using FluentValidation.AspNetCore;
using Backend.Features.Review.DTO;
using Backend.Features.Shared.IAM.DTO;
using Serilog;
using DotNetEnv;
using Backend.Features.Shared.StripeService.HandleStripeWebhook;
using Backend.Features.Reports.CreateReport;
using Backend.Features.Reports.DTO;
using Backend.Features.Reports.GetAcceptedReportsCount;
using Backend.Features.Reports.GetAllReports;
using Backend.Features.Reports.GetReportsByItem;
using Backend.Features.Reports.GetReportsByModerator;
using Backend.Features.Reports.UpdateReportStatus;
using Backend.Features.Review.CreateReview;
using Backend.Features.Review.DeleteReview;
using Backend.Features.Review.GetAllReviews;
using Backend.Features.Review.GetReview;
using Backend.Features.Review.UpdateReview;
using Backend.Features.Shared.Authorization.ModeratorAuthorization;
using Backend.Features.Shared.IAM.AssignAdminRole;
using Backend.Features.Shared.IAM.ChangePassword;
using Backend.Features.Shared.IAM.GetRefreshTokens;
using Backend.Features.Shared.IAM.RefreshToken;
using Backend.Features.Shared.IAM.RequestPasswordReset;
using Backend.Features.Shared.IAM.SendEmailVerification;
using Backend.Features.Shared.IAM.VerifyPasswordReset;
using Backend.Features.Shared.Pipeline.Logging;
using Backend.Features.Shared.Pipeline.Validation;
using Backend.Features.Shared.StripeService.CreateCheckoutSession;
using Backend.Features.Shared.StripeService.CreateStripeAccountLink;
using Backend.Features.Shared.StripeService.DTO;
using Backend.Features.Universities.GetAllUniversities;
using Backend.Features.Universities.PostUniversities;
using Backend.Features.Users.AssignModeratorRole;
using Backend.Features.Users.ConfirmEmail;
using Backend.Features.Users.DeleteUser;
using Backend.Features.Users.DTO;
using Backend.Features.Users.GetAllUserBookedItems;
using Backend.Features.Users.GetAllUsers;
using Backend.Features.Users.GetUser;
using Backend.Features.Users.GetUserBookedItem;
using Backend.Features.Users.GetUserBookings;
using Backend.Features.Users.LoginUser;
using Backend.Features.Users.RegisterUser;
using Backend.Features.Users.UpdateUser;
using Backend.Mappers.Booking;
using Backend.Mappers.Review;
using Backend.Mappers.University;
using Backend.Mappers.User;
using Backend.Mapping;
using Backend.Services.EmailSender;
using Backend.Services.Hashing;
using Backend.Services.Token;
//comment for testing sonarqube
// Configure Serilog before building the application
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/unishare-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10_485_760, // 10 MB
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 30,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("Starting UniShare API application");

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

var frontendOrigin = Environment.GetEnvironmentVariable("API_FRONTEND_URL");
if (string.IsNullOrEmpty(frontendOrigin))
{
    throw new InvalidOperationException("Configuration value 'FrontendOrigin' is missing.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // 1. USE SetIsOriginAllowed instead of AllowAnyOrigin
        // This dynamically checks the origin and allows it, bypassing the "*" restriction
        policy.SetIsOriginAllowed(origin => true) 
              
            .AllowAnyHeader()
            .AllowAnyMethod()
              
            // 2. THIS IS MANDATORY for SignalR
            .AllowCredentials(); 
    });

    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UniShare API",
        Version = "v1",
        Description = "API for managing the UniShare application",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com",
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSignalR();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["JwtSettings:Key"] ??
                     throw new InvalidOperationException("Configuration value 'JwtSettings:Key' is missing.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat")))
                {
                    // Read the token from the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
        
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    // Add logging behavior to MediatR pipeline
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.EnvironmentName != "Testing")
{
    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Warning("No database connection string configured. Database features will not be available.");
    }

    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseNpgsql(connectionString ?? ""));
}

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<UserMapper>();
        cfg.AddProfile<UniversityMapper>();
        cfg.AddProfile<ItemMapper>();
        cfg.AddProfile<BookingMapper>();
        cfg.AddProfile<ReviewMapper>();
        cfg.AddProfile<Backend.Mappers.Report.ReportMapper>();
        cfg.AddProfile<Backend.Mappers.ModeratorAssignment.ModeratorAssignmentMapper>();
    },
    typeof(Backend.Mappers.Report.ReportMapper), typeof(Backend.Mappers.ModeratorAssignment.ModeratorAssignmentMapper));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<IUserValidator<User>, EmailValidator>();
builder.Services.AddScoped<CreateBookingHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBookingStatusRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateModeratorAssignmentRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateReportRequest>();


builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

// Initialize roles and seed database
var dbConnectionString = app.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(dbConnectionString))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationContext>();

        if (app.Environment.EnvironmentName != "Testing")
        {
            // Apply pending migrations
            if (context.Database.IsRelational())
            {
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                // If we run the app normally, apply migrations
                await context.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
            }
            else
            {
                // If we run the tests with InMemory database, ensure created
                await context.Database.EnsureCreatedAsync();
            }
        }
    }
}
else
{
    Log.Warning("Skipping database initialization - no connection string configured");
}

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI
(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniShare API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        c.DisplayRequestDuration();
    }
);
app.MapOpenApi();

app.UseCors("AllowAll");

// Serve static files (for uploaded chat images)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

/// Auth Endpoints - Anonymous access
var authGroup = app.MapGroup("/auth")
    .WithTags("Auth");

// Root auth endpoints (not nested under /auth/)
app.MapPost("/login", async (LoginUserDto dto, IMediator mediator) =>
        await mediator.Send(new LoginUserRequest(dto.Email, dto.Password)))
    .WithTags("Auth")
    .AllowAnonymous();

app.MapPost("/refresh", async (RefreshTokenDto dto, IMediator mediator) =>
        await mediator.Send(new RefreshTokenRequest(dto.RefreshToken)))
    .WithTags("Auth")
    .AllowAnonymous();

app.MapPost("/register", async (RegisterUserDto dto, IMediator mediator) =>
        await mediator.Send(new RegisterUserRequest(dto)))
    .WithTags("Auth")
    .AllowAnonymous();

app.MapHub<Backend.Hubs.ChatHub>("/hubs/chat");

authGroup.MapPost("/verification-code", async (SendEmailVerificationDto dto, IMediator mediator) =>
        await mediator.Send(new SendEmailVerificationRequest(dto.UserId)))
    .RequireAuthorization()
    .AllowAdmin()
    .RequireOwner();

authGroup.MapPost("/email-confirmation", async (ConfirmEmailDto dto, IMediator mediator) =>
        await mediator.Send(new ConfirmEmailRequest(dto.UserId, dto.Code)))
    .AllowAnonymous();

// Password reset endpoints
authGroup.MapPost("/password-reset/request", async (RequestPasswordResetDto dto, IMediator mediator) =>
        await mediator.Send(new RequestPasswordResetRequest(dto.Email)))
    .WithDescription("Request a password reset code to be sent via email")
    .AllowAnonymous();

authGroup.MapGet("/password", async (Guid userId, string code, IMediator mediator) =>
        await mediator.Send(new VerifyPasswordResetRequest(userId, code)))
    .WithDescription("Verify password reset code and get temporary token")
    .AllowAnonymous();

/// Users Endpoints
var usersGroup = app.MapGroup("/users")
    .WithTags("Users")
    .RequireAuthorization();

// Password change endpoint (requires temporary password reset token or admin)
usersGroup.MapPost("/password", async (ChangePasswordDto dto, IMediator mediator) =>
        await mediator.Send(new ChangePasswordRequest(dto)))
    .WithDescription("Change user password (requires temporary password reset token or admin)")
    .AllowAdmin()
    .RequirePasswordResetToken();

// Admin only - list all users
usersGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllUsersRequest()))
    .RequireAdmin();

// Admin only - get all admins
usersGroup.MapGet("/admins", async (IMediator mediator) =>
        await mediator.Send(new GetAdminsRequest()))
    .WithDescription("Get all admin users with their IDs and emails (Admin only)")
    .RequireAdmin();

// Admin only - get all moderators
usersGroup.MapGet("/moderators", async (IMediator mediator) =>
        await mediator.Send(new GetModeratorsRequest()))
    .WithDescription("Get all moderator users with their IDs and emails (Admin only)")
    .RequireAdmin();

// User-specific routes that require owner or admin
var userByIdGroup = usersGroup.MapGroup("/{userId:guid}")
    .AllowAdmin();

userByIdGroup.MapGet("", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new GetUserRequest(userId)))
    .RequireOwner();

userByIdGroup.MapPatch("", async (Guid userId, UpdateUserDto dto, IMediator mediator) =>
        await mediator.Send(new UpdateUserRequest(userId, dto)))
    .WithDescription("Update user information (PATCH)")
    .RequireOwner();

userByIdGroup.MapGet("/refresh-tokens", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new GetRefreshTokensRequest(userId)))
    .RequireOwner();

userByIdGroup.MapDelete("",
        async (Guid userId, IMediator mediator) => await mediator.Send(new DeleteUserRequest(userId)))
    .RequireOwner();

userByIdGroup.MapPost("/assign-admin", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new AssignAdminRoleRequest(userId)))
    .WithDescription("Assign admin role to a user (Admin only)")
    .RequireAdmin();

userByIdGroup.MapPost("/assign-moderator", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new AssignModeratorRoleRequest(userId)))
    .WithDescription("Assign moderator role to a user (Admin only)")
    .RequireAdmin();

// User-specific routes that require owner + email verification (or admin)
var userVerifiedGroup = usersGroup.MapGroup("/{userId:guid}")
    .AllowAdmin()
    .RequireOwner()
    .RequireEmailVerification();

userVerifiedGroup.MapGet("/items", async (Guid userId, IMediator mediator) =>
    await mediator.Send(new GetAllUserItemsRequest(userId)));

userVerifiedGroup.MapGet("/items/{itemId:guid}", async (Guid userId, Guid itemId, IMediator mediator) =>
    await mediator.Send(new GetUserItemRequest(userId, itemId)));

userVerifiedGroup.MapGet("/bookings", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new GetUserBookingsRequest(userId)))
    .WithDescription("Get all bookings for a specific user");

userVerifiedGroup.MapGet("/booked-items", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new GetAllUserBookedItemsRequest(userId)))
    .WithDescription("Get all items booked by a specific user");

userVerifiedGroup.MapGet("/booked-items/{bookingId:guid}", async (Guid userId, Guid bookingId, IMediator mediator) =>
        await mediator.Send(new GetUserBookedItemRequest(userId, bookingId)))
    .WithDescription("Get a specific booked item for a user");

/// Items Endpoints
var itemsGroup = app.MapGroup("/items")
    .WithTags("Items");

itemsGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllItemsRequest()))
    .AllowAnonymous();

itemsGroup.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new GetItemRequest(id)))
    .AllowAnonymous();

itemsGroup.MapPost("", async (PostItemRequest request, IMediator mediator) =>
        await mediator.Send(request))
    .RequireAuthorization()
    .AllowAdmin()
    .RequireEmailVerification();

itemsGroup.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new DeleteItemRequest(id)))
    .RequireAuthorization()
    .AllowAdmin()
    .RequireEmailVerification();

itemsGroup.MapGet("/{id:guid}/bookings", async (Guid id, IMediator mediator) =>
        await mediator.Send(new GetBookingsForItemRequest(id)))
    .AllowAnonymous();

/// Universities Endpoints
var universitiesGroup = app.MapGroup("/universities")
    .WithTags("Universities");

universitiesGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllUniversitiesRequest()))
    .WithName("GetUniversities")
    .AllowAnonymous();

universitiesGroup.MapPost("", async (PostUniversitiesRequest request, IMediator mediator) =>
        await mediator.Send(request))
    .RequireAuthorization()
    .RequireAdmin();

/// Bookings Endpoints
var bookingsGroup = app.MapGroup("/bookings")
    .WithTags("Bookings")
    .RequireAuthorization();

bookingsGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllBookingsRequest()))
    .WithDescription("Get all bookings in the system (Admin only)")
    .RequireAdmin();

// Booking operations requiring email verification (or admin)
var bookingVerifiedGroup = app.MapGroup("/bookings")
    .WithTags("Bookings")
    .RequireAuthorization()
    .AllowAdmin()
    .RequireEmailVerification();

bookingVerifiedGroup.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new GetBookingRequest(id)))
    .WithDescription("Get a specific booking by ID");

bookingVerifiedGroup.MapPost("", async (CreateBookingDto dto, IMediator mediator) =>
        await mediator.Send(new CreateBookingRequest(dto)))
    .WithDescription("Create a new booking")
    .RequireAuthorization();

bookingVerifiedGroup.MapPatch("/{id:guid}",
        async (Guid id, UpdateBookingStatusDto bookingStatusDto, IMediator mediator) =>
            await mediator.Send(new UpdateBookingStatusRequest(id, bookingStatusDto)))
    .WithDescription("Update the status of a booking")
    .RequireAuthorization();

bookingVerifiedGroup.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new DeleteBookingRequest(id)))
    .WithDescription("Delete a booking")
    .RequireAuthorization();


/// Stripe Endpoints
var stripeGroup = app.MapGroup("/stripe")
    .WithTags("Stripe")
    .RequireAuthorization();

// Webhook endpoint - must be anonymous for Stripe to call it
app.MapPost("/stripe/webhook", async (HttpContext httpContext, IMediator mediator) =>
    {
        var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = httpContext.Request.Headers["Stripe-Signature"].ToString();
        return await mediator.Send(new HandleStripeWebhookRequest(json, signatureHeader));
    })
    .WithTags("Stripe")
    .AllowAnonymous()
    .WithDescription("Stripe webhook endpoint for payment events");

// Create Stripe Connect account link for onboarding
stripeGroup.MapPost("/connect/account-link", async (CreateStripeAccountLinkDto dto, IMediator mediator) =>
        await mediator.Send(new CreateStripeAccountLinkRequest(dto)))
    .WithDescription("Create a Stripe Connect account onboarding link for a user to become a seller")
    .RequireEmailVerification();

// Create checkout session for booking payment
stripeGroup.MapPost("/checkout", async (CreateCheckoutSessionDto dto, IMediator mediator) =>
        await mediator.Send(new CreateCheckoutSessionRequest(dto)))
    .WithDescription("Create a Stripe checkout session for booking payment")
    .RequireEmailVerification();


/// Reviews Endpoints
var reviewsGroup = app.MapGroup("/reviews")
    .WithTags("Reviews")
    .RequireAuthorization();

reviewsGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllReviewsRequest()))
    .WithDescription("Get all reviews")
    .AllowAnonymous();

reviewsGroup.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new GetReviewRequest(id)))
    .WithDescription("Get a specific review by ID")
    .AllowAnonymous();

reviewsGroup.MapPost("", async (CreateReviewDto dto, IMediator mediator) =>
        await mediator.Send(new CreateReviewRequest(dto)))
    .WithDescription("Create a new review")
    .RequireEmailVerification();

reviewsGroup.MapPatch("/{id:guid}", async (Guid id, UpdateReviewDto dto, IMediator mediator) =>
        await mediator.Send(new UpdateReviewRequest(id, dto)))
    .WithDescription("Update an existing review's rating and comment")
    .RequireEmailVerification();
// Backwards-compatible PUT endpoint (some clients still send PUT instead of PATCH)
reviewsGroup.MapPut("/{id:guid}", async (Guid id, UpdateReviewDto dto, IMediator mediator) =>
        await mediator.Send(new UpdateReviewRequest(id, dto)))
    .WithDescription("(Compatibility) Update an existing review's rating and comment via PUT")
    .RequireEmailVerification();

reviewsGroup.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        await mediator.Send(new DeleteReviewRequest(id)))
    .WithDescription("Delete a review")
    .RequireEmailVerification();

/// Reports Endpoints
var reportsGroup = app.MapGroup("/reports")
    .WithTags("Reports")
    .RequireAuthorization();

// Create a new report
reportsGroup.MapPost("", async (CreateReportDto dto, IMediator mediator) =>
        await mediator.Send(new CreateReportRequest(dto)))
    .WithDescription("Create a new report for an item")
    .RequireEmailVerification();

// Get all reports (Admin only)
reportsGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllReportsRequest()))
    .WithDescription("Get all reports in the system (Admin only)")
    .RequireAdmin();

// Get reports by item ID
reportsGroup.MapGet("/item/{itemId:guid}", async (Guid itemId, IMediator mediator) =>
        await mediator.Send(new GetReportsByItemRequest(itemId)))
    .WithDescription("Get all reports for a specific item")
    .RequireAdmin();

// Get reports by moderator ID
reportsGroup.MapGet("/moderator/{moderatorId:guid}", async (Guid moderatorId, IMediator mediator) =>
        await mediator.Send(new GetReportsByModeratorRequest(moderatorId)))
    .WithDescription("Get all reports handled by a specific moderator")
    .RequireAdminOrModerator();

// Get accepted reports count from last week for an item
reportsGroup.MapGet("/item/{itemId:guid}/accepted-last-week",
        async (Guid itemId, int numberOfDays, IMediator mediator) =>
            await mediator.Send(new GetAcceptedReportsCountLastWeekRequest(itemId, numberOfDays)))
    .WithDescription("Get the number of accepted reports from the specified period of time for a specific item")
    .AllowAnonymous();

// Update report status (Admin or Moderator)
reportsGroup.MapPatch("/{reportId:guid}", async (Guid reportId, UpdateReportStatusDto dto, IMediator mediator) =>
        await mediator.Send(new UpdateReportStatusRequest(reportId, dto)))
    .WithDescription("Update the status of a report (Admin or Moderator)")
    .RequireAdminOrModerator();

/// Moderator Assignment Endpoints
var moderatorAssignmentsGroup = app.MapGroup("/moderator-assignments")
    .WithTags("ModeratorAssignments")
    .RequireAuthorization();

// Submit a moderator assignment
moderatorAssignmentsGroup.MapPost("", async (CreateModeratorAssignmentDto dto, IMediator mediator) =>
        await mediator.Send(new CreateModeratorAssignmentRequest(dto)))
    .WithDescription("Submit a request to become a moderator")
    .RequireEmailVerification();

// Get all moderator assignments (Admin only)
moderatorAssignmentsGroup.MapGet("", async (IMediator mediator) =>
        await mediator.Send(new GetAllModeratorAssignmentsRequest()))
    .WithDescription("Get all moderator assignments in the system (Admin only)")
    .RequireAdmin();

// Update moderator assignment status (Admin only)
moderatorAssignmentsGroup.MapPatch("/{assignmentId:guid}",
        async (Guid assignmentId, UpdateModeratorAssignmentStatusDto dto, IMediator mediator) =>
            await mediator.Send(new UpdateModeratorAssignmentStatusRequest(assignmentId, dto)))
    .WithDescription("Update the status of a moderator assignment (Admin only)")
    .RequireAdmin();

// Add this inside a "Chat" group in Program.cs
var chatGroup = app.MapGroup("/chat").RequireAuthorization();

chatGroup.MapGet("/history/{otherUserId:guid}", async (Guid otherUserId, ClaimsPrincipal user, ApplicationContext db) =>
{
    var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var messages = await db.ChatMessages
        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) || 
                    (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
        .OrderBy(m => m.Timestamp)
        .Select(m => new {
            m.SenderId,
            m.Content,
            m.ImageUrl,
            MessageType = m.MessageType.ToString(),
            m.Timestamp
        })
        .ToListAsync();

    return Results.Ok(messages);
});

// Upload image for chat
chatGroup.MapPost("/upload-image", async (HttpRequest request, IWebHostEnvironment env) =>
{
    try
    {
        if (!request.HasFormContentType)
        {
            Log.Warning("Upload-image: request did not have form content type");
            return Results.BadRequest("Expected form content type");
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("image");

        if (file == null || file.Length == 0)
        {
            Log.Warning("Upload-image: no file provided or empty file");
            return Results.BadRequest("No image provided");
        }

        // Normalize content type and file extension
        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/pjpeg", "image/png", "image/x-png", "image/gif", "image/webp" };
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Try to read extension from the uploaded filename
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;

        // If the content-type is not in the allowed list, we'll still try to infer from extension
        if (!allowedContentTypes.Contains(contentType) && string.IsNullOrEmpty(fileExtension))
        {
            Log.Warning("Upload-image: unknown content type and no extension; ContentType={ContentType}, FileName={FileName}", contentType, file.FileName);
            return Results.BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed.");
        }

        // Map some common content types to extensions if extension is missing or unexpected
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "image/jpeg", ".jpg" },
                { "image/jpg", ".jpg" },
                { "image/pjpeg", ".jpg" },
                { "image/png", ".png" },
                { "image/x-png", ".png" },
                { "image/gif", ".gif" },
                { "image/webp", ".webp" }
            };

            if (map.TryGetValue(contentType, out var extFromContentType))
            {
                fileExtension = extFromContentType;
            }
        }

        // Final validation by extension
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
        {
            Log.Warning("Upload-image: invalid file type after checks; ContentType={ContentType}, FileName={FileName}, Extension={Extension}", contentType, file.FileName, fileExtension);
            return Results.BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed.");
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            Log.Warning("Upload-image: file too large ({Length} bytes)", file.Length);
            return Results.BadRequest("File too large. Maximum size is 5MB.");
        }

        // Create uploads directory if it doesn't exist
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        Log.Information("Upload-image: WebRootPath={WebRoot}", webRoot);
        var uploadsPath = Path.Combine(webRoot, "uploads", "chat");
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsPath, safeFileName);

        // Save the file
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return the URL
        var imageUrl = $"/uploads/chat/{safeFileName}";
        Log.Information("Upload-image: saved {ImageUrl} (ContentType={ContentType}, OriginalFileName={OriginalFileName})", imageUrl, contentType, file.FileName);
        return Results.Ok(new { imageUrl });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Upload-image: unexpected error");
        return Results.StatusCode(500);
    }
}).DisableAntiforgery();

// Get list of conversations (users we've chatted with)
chatGroup.MapGet("/conversations", async (ClaimsPrincipal user, ApplicationContext db) =>
{
    var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // Get all unique user IDs we've chatted with
    var sentToUserIds = db.ChatMessages
        .Where(m => m.SenderId == currentUserId)
        .Select(m => m.ReceiverId);

    var receivedFromUserIds = db.ChatMessages
        .Where(m => m.ReceiverId == currentUserId)
        .Select(m => m.SenderId);

    var allUserIds = await sentToUserIds.Union(receivedFromUserIds).Distinct().ToListAsync();

    // Get user details and last message for each conversation
    var conversations = new List<object>();
    foreach (var otherUserId in allUserIds)
    {
        var otherUser = await db.Users.FindAsync(otherUserId);
        if (otherUser == null) continue;

        var lastMessage = await db.ChatMessages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        conversations.Add(new
        {
            UserId = otherUserId,
            UserName = $"{otherUser.FirstName} {otherUser.LastName}",
            UserEmail = otherUser.Email,
            LastMessage = lastMessage?.Content,
            LastMessageTime = lastMessage?.Timestamp,
            LastMessageSenderId = lastMessage?.SenderId
        });
    }

    // Sort by last message time (most recent first)
    return Results.Ok(conversations.OrderByDescending(c => ((dynamic)c).LastMessageTime));
});


// Log the URLs where the application is listening

Log.Information("UniShare API started successfully");

await app.RunAsync();

public partial class Program
{
}

