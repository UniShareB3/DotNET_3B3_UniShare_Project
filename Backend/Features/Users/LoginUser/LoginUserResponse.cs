namespace Backend.Features.Users;

public record LoginUserResponse(
    string AccessToken, 
    string RefreshToken, 
    int ExpiresIn,
    string TokenType = "Bearer"
);

