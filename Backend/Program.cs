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
using Backend.Features.Booking;
using Backend.Features.Booking.DTO;
using Backend.Features.Shared.Pipeline;
using Backend.Features.Users;
using Backend.Features.Users.Dtos;
using Backend.Mapper;
using MediatR;

using FluentValidation.AspNetCore;
using Backend.Features.Bookings;
using Backend.Features.Bookings.DTO;
using Backend.Mapping;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc
    (
        "v1",
        new OpenApiInfo
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
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("Configuration value 'JwtSettings:Key' is missing.");
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

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<CreateBookingMapping>(), typeof(CreateBookingMapping));
builder.Services.AddAutoMapper(cfg=>
{
    cfg.AddProfile<ItemProfile>();
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<IHashingService, HashingService>();

builder.Services.AddScoped<IUserValidator<User>, EmailValidator>();
builder.Services.AddScoped<CreateBookingHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequest>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBookingStatusRequest>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();
app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI
    (c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniShare API V1");
            c.RoutePrefix = string.Empty; 
            c.DisplayRequestDuration();
        }
    );
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginUserDto dto, IMediator mediator) => 
    await mediator.Send(new LoginUserRequest(dto.Email, dto.Password)));
app.MapPost("/refresh", async (RefreshTokenDto dto, IMediator mediator) => 
    await mediator.Send(new RefreshTokenRequest(dto.RefreshToken)));
app.MapPost("/register", async (RegisterUserDto dto, IMediator mediator) => 
    await mediator.Send(new RegisterUserRequest(dto.Email, dto.FirstName, dto.LastName, dto.Password)));
app.MapPost("/auth/send-verification-code", async (SendEmailVerificationDto dto, IMediator mediator) => 
    await mediator.Send(new SendEmailVerificationRequest(dto.UserId)));
app.MapPost("/auth/confirm-email", async (ConfirmEmailDto dto, IMediator mediator) => 
    await mediator.Send(new ConfirmEmailRequest(dto.UserId, dto.Code)));

app.MapGet("/users", async (IMediator mediator) => await mediator.Send(new GetAllUsersRequest()));
app.MapGet("/users/{userId:guid}", async (Guid userId, IMediator mediator) => await mediator.Send(new GetUserRequest(userId)));
app.MapGet("/users/{userId:guid}/refresh-tokens", async (Guid userId, IMediator mediator) => 
    await mediator.Send(new GetRefreshTokensRequest(userId)));
app.MapGet("users/{userId:guid}/items", async (Guid userId, IMediator mediator) => 
    await mediator.Send(new GetAllUserItemsRequest(userId)));
app.MapGet("users/{userId:guid}/items/{itemId:guid}", async (Guid userId, Guid itemId, IMediator mediator) => 
    await mediator.Send(new GetUserItemRequest(userId, itemId)));
app.MapGet("users/{userId:guid}/bookings", async (Guid userId, IMediator mediator) => 
    await mediator.Send(new GetUserBookingsRequest(userId)));
app.MapGet("users/{userId:guid}/booked-items", async (Guid userId, IMediator mediator) => 
    await mediator.Send(new  GetAllUserBookedItemsRequest(userId)));
app.MapGet("users/{userId:guid}/booked-items/{bookingId:guid}", async (Guid userId, Guid bookingId, IMediator mediator) => 
    await mediator.Send(new  GetUserBookedItemRequest(userId, bookingId)));
app.MapDelete("/users/{userId:guid}", async (Guid userId, IMediator mediator) => 
    await mediator.Send(new DeleteUserRequest(userId)));

app.MapGet("/items", async (IMediator mediator) => await mediator.Send(new GetAllItemsRequest()));
app.MapGet("items/{id:guid}", async (Guid id, IMediator mediator) => await mediator.Send(new GetItemRequest(id)));
app.MapPost("items", async (PostItemRequest request, IMediator mediator) =>  await mediator.Send(request));
app.MapDelete("items/{id:guid}", async (Guid id, IMediator mediator) => await mediator.Send(new DeleteItemRequest(id)));

app.MapGet("/bookings", async (IMediator mediator) => await mediator.Send(new GetAllBookingsRequest()));
app.MapGet("/bookings/{id:guid}", async (Guid id, IMediator mediator) => await mediator.Send(new GetBookingRequest(id)));
app.MapPost( "/bookings", async (CreateBookingDto dto, IMediator mediator) => 
    await mediator.Send(new CreateBookingRequest(dto)));
app.MapPatch("/bookings/{id:guid}", async (Guid id, UpdateBookingStatusDto bookingStatusDto, IMediator mediator) => 
    await mediator.Send(new UpdateBookingStatusRequest(id, bookingStatusDto)));
app.MapDelete("/bookings/{id:guid}", async (Guid id, IMediator mediator) => await mediator.Send(new DeleteBookingRequest(id)));

await app.RunAsync();

public partial class Program { }
