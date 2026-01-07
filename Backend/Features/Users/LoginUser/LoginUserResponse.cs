namespace Backend.Features.Users.LoginUser;

public record LoginUserResponse(
    string AccessToken, 
    string RefreshToken, 
    int ExpiresIn,
    string TokenType = "Bearer"
);

