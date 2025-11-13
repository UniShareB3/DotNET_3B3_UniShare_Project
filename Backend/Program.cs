using Backend.Features.Items;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;

using Backend.TokenGenerators;
using Backend.Validators;
using Backend.Services;

using Backend.Data;
using Backend.Features.Users;

var builder = WebApplication.CreateBuilder(args);

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<GetAllItemsHandler>();
builder.Services.AddScoped<GetItemHandler>();
builder.Services.AddScoped<PostItemHandler>();
builder.Services.AddScoped<DeleteItemHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<GetAllUsersHandler>();
builder.Services.AddScoped<GetUserHandler>();
builder.Services.AddScoped<GetRefreshTokensByEmailHandler>();
builder.Services.AddScoped<SendEmailVerificationHandler>();
builder.Services.AddScoped<ConfirmEmailHandler>();
builder.Services.AddScoped<DeleteUserHandler>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<GetAllUserItemsHandler>();
builder.Services.AddScoped<GetUserItemHandler>();

builder.Services.AddScoped<IUserValidator<User>, EmailValidator>();

var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginUserRequest request, LoginUserHandler handler) => await handler.Handle(request));
app.MapPost("/refresh", async (RefreshTokenRequest request, RefreshTokenHandler handler, HttpContext httpContext) => 
    await handler.Handle(request, httpContext));
app.MapPost("/register", async (RegisterUserRequest request, RegisterUserHandler handler) => await handler.Handle(request));
app.MapPost("/auth/send-verification-code", async (SendEmailVerificationRequest request, SendEmailVerificationHandler handler) => 
    await handler.Handle(request));
app.MapPost("/auth/confirm-email", async (ConfirmEmailRequest request, ConfirmEmailHandler handler) => 
    await handler.Handle(request));
app.MapGet("/users", async (GetAllUsersHandler handler) => await handler.Handle(new GetAllUsersRequest()));
app.MapGet("/users/{email}", async (string email, GetUserHandler handler) => await handler.Handle(new GetUserByEmailRequest(email)));
app.MapGet("/users/{email}/refresh-tokens", async (string email, GetRefreshTokensByEmailHandler handler) => 
    await handler.Handle(new GetRefreshTokensByEmailRequest(email)));
app.MapDelete("/users/{email}", async (string email, DeleteUserHandler handler) => 
    await handler.Handle(new DeleteUserRequest(email)));
app.MapGet("users/{userId:guid}/items", async (Guid userId, GetAllUserItemsHandler handler) => 
    await handler.Handle(new GetAllUserItemsRequest(userId)));
app.MapGet("users/{userId:guid}/items/{itemId:guid}", async (Guid userId, Guid itemId, GetUserItemHandler handler) => 
    await handler.Handle(new GetUserItemRequest(userId, itemId)));
app.MapGet("/items", async (GetAllItemsHandler handler) => await handler.Handle());
app.MapGet("items/{id:guid}", async (Guid id, GetItemHandler handler) => await handler.Handle(new GetItemRequest(id)));
app.MapPost("items", async (PostItemRequest request, PostItemHandler handler) =>  await handler.Handle(request));
app.MapDelete("items/{id:guid}", async (Guid id, DeleteItemHandler handler) => await handler.Handle(new DeleteItemRequest(id)));
await app.RunAsync();
