# UniShare – Student-to-Student Lending Platform

## 📘 Project Overview
UniShare is a web platform that allows university students to lend and borrow items (books, electronics, etc.) among themselves. It features secure authentication, item management, booking workflows, a review system, and real-time messaging.

## 🧪 Tech Stack
- **Backend:** .NET 9 Minimal API
- **Architecture:** Vertical Slice Architecture
- **Database:** PostgreSQL (Npgsql)
- **Object-Relational Mapper:** EF Core 9
- **Real-time Communication:** SignalR (ChatHub)
- **Storage:** Azure Blob Storage (for images/documents)
- **Identity:** ASP.NET Core Identity
- **Validation:** FluentValidation
- **Docs/API Testing:** Swagger (OpenAPI)
- **Testing:** XUnit, NSubstitute

## 🧩 Project Structure
The project follows a **Vertical Slice Architecture**, organizing code by feature rather than technical layer.

```
Backend/
├── Data/                   # Database Context & Seeding
├── Features/               # Vertical Slices
│   ├── Bookings/           # Booking management
│   ├── Conversations/      # Real-time Chat
│   ├── Items/              # Item listings
│   ├── ModeratorAssignment/# Moderator requests
│   ├── Reports/            # Content reporting
│   ├── Review/             # Ratings & Reviews
│   ├── Universities/       # University management
│   └── Users/              # User profiles & auth
├── Hubs/                   # SignalR Hubs (ChatHub)
├── Persistence/            # DB Configuration
├── Services/               # Infrastructure services (Email, Storage, etc.)
└── Program.cs              # App entry point & Configuration
```

## 💡 Code Hints

### 🔹 Vertical Slice Example (Primary Constructors & Minimal API)
```csharp
// Request
public record CreateBookingRequest(CreateBookingDto Booking) : IRequest<IResult>;

// Handler
public class CreateBookingHandler(ApplicationContext dbContext, IMapper mapper) 
    : IRequestHandler<CreateBookingRequest, IResult> 
{
    public async Task<IResult> Handle(CreateBookingRequest request, CancellationToken ct) 
    {
        var booking = mapper.Map<Data.Booking>(request.Booking);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(ct);
        
        return Results.Created($"/bookings/{booking.Id}", booking);
    }
}
```

### 🔹 EF Core Setup
```csharp
public class ApplicationContext(DbContextOptions<ApplicationContext> options) : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options) 
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Booking> Bookings => Set<Booking>();
    // ...
}
```

### 🔹 FluentValidation
```csharp
public class CreateBookingValidator : AbstractValidator<CreateBookingRequest> 
{
    public CreateBookingValidator() 
    {
        RuleFor(x => x.Booking.ItemId).NotEmpty();
        RuleFor(x => x.Booking.StartDate).LessThan(x => x.Booking.EndDate);
    }
}
```

## 🚀 Core Features
- **Items:** Create, Read, Update, Delete listings with image support.
- **Bookings:** Request items, manage status (Approve/Complete), history.
- **Reviews:** Rate users and items after transactions.
- **Chat:** Real-time messaging between users via SignalR.
- **Moderation:** Moderator assignments, Reporting system for items/users.
- **Authentication:** Register, Login, Email Verification, Password Reset (Identity + JWT).
- **Universities:** Management of university domains/data.

## 👥 Team Responsibilities
- **Dev 1:** Auth + Setup + Unit Tests
- **Dev 2:** Items + Bookings + Validation + Unit Tests
- **Dev 3:** Reviews + EF Core + Integration Tests
- **Dev 4:** Blazor UI + End-to-End Tests + Docs


## 📦 Deployment
- **Docker:** Containerized application (Dockerfile included).
- **Cloud:** Render Web Apps / Azure Blob Storage.

## 👥 Team Members
1. Ciornei Stefan-Alexandru
2. Lungu Fabian
3. Marciuc Teodor-Cosmin
4. Milea Bianca-Elena


**Docs Link:** [Project Documentation](https://docs.google.com/document/d/1vOM7MuORHe_u26Nk7aR6LKtZRHFtBV1RbVDtZKstCVM/edit?usp=sharing)
