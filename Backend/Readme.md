# UniShare Backend

ASP.NET Core Web API for the UniShare platform.

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database
- Gmail account with 2-Step Verification enabled (for email functionality)

## Setup

### 1. Database Configuration

Update the connection string in `appsettings.json` if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=UniShare;Username=postgres;Password=admin"
}
```

### 2. Run Database Migrations

```bash
dotnet ef database update
```

### 3. SMTP Configuration

The application uses Gmail SMTP for sending emails. Configure the SMTP settings in `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "UseSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "",
  "From": "your-email@gmail.com"
}
```

**Important:** Do NOT store your password in `appsettings.json`. Use dotnet user-secrets instead (see below).

### 4. Generate Gmail App Password

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable **2-Step Verification** (required)
3. Go to [App passwords](https://myaccount.google.com/apppasswords)
4. Select app and device (or choose "Other" to name it)
5. Click **Generate**
6. Copy the 16-character password (displayed as four groups of four characters)

### 5. Configure User Secrets (Recommended)

Store sensitive data like the Gmail app password using dotnet user-secrets:

```bash
# Initialize user secrets for the project
dotnet user-secrets init

# Set the SMTP password (replace with your 16-character app password)
dotnet user-secrets set "Smtp:Password" "abcdabcdabcdabcd"

# Set the SMTP username if different
dotnet user-secrets set "Smtp:Username" "your-email@gmail.com"

# Verify secrets are set
dotnet user-secrets list

# (Optional) Remove a secret
dotnet user-secrets remove "Smtp:Password"

# (Optional) Clear all secrets
dotnet user-secrets clear
```

User secrets are stored locally on your machine and are automatically loaded in the `Development` environment. They will not be committed to source control.

### 6. JWT Configuration

The JWT settings are already configured in `appsettings.json`. For production, consider moving the `Key` to user secrets or environment variables:

```bash
dotnet user-secrets set "JwtSettings:Key" "your-secure-key-here"
```

## Running the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in `launchSettings.json`).

## Development

### Project Structure

- **Data/** - Entity models
- **Features/** - Feature-based organization (handlers and requests)
- **Migrations/** - EF Core database migrations
- **Persistence/** - Database context
- **Services/** - Application services (hashing, email, tokens)
- **Validators/** - Input validation

### Testing API Endpoints

Use the `Backend.http` file with JetBrains Rider or VS Code REST Client extension for quick API testing.

## Security Notes

- Never commit secrets to source control
- Use user secrets for local development
- Use environment variables or Azure Key Vault for production
- Rotate JWT keys and app passwords regularly
- Revoke app passwords if compromised