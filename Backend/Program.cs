using Backend.Features.Items;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Backend.TokenGenerators;
using Backend.Validators;
using Backend.Services;
using Backend.Data;
using Backend.Features.Universities;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Features.Shared.Pipeline;
using Backend.Features.Shared.Authorization;
using Backend.Features.Users;
using Backend.Features.Users.Dtos;
using MediatR;
using FluentValidation.AspNetCore;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Features.Review;
using Backend.Features.Review.DTO;
using Backend.Features.Shared.Auth;
using Backend.Features.Shared.IAM.DTO;
using Backend.Mapping;
using Serilog;

// Configure Serilog before building the application
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
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

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

var frontendOrigin = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:5083";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
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
}, typeof(UserMapper), typeof(UniversityMapper), typeof(ItemMapper), typeof(BookingMapper), typeof(ReviewMapper));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<IUserValidator<User>, EmailValidator>();
builder.Services.AddScoped<CreateBookingHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBookingStatusRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordRequest>();
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

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
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
}

//app.UseHttpsRedirection();
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

userByIdGroup.MapDelete("", async (Guid userId, IMediator mediator) =>  await mediator.Send(new DeleteUserRequest(userId)))
    .RequireOwner();

userByIdGroup.MapPost("/assign-admin", async (Guid userId, IMediator mediator) =>
        await mediator.Send(new AssignAdminRoleRequest(userId)))
    .WithDescription("Assign admin role to a user (Admin only)")
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

reviewsGroup.MapPost("", async (CreateReviewDTO dto, IMediator mediator) =>
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

// Log the URLs where the application is listening

Log.Information("UniShare API started successfully");

var url = "http://localhost:5083/index.html";
Log.Information("Application is listening on: {Url}", url);

await app.RunAsync();

public partial class Program
{
}