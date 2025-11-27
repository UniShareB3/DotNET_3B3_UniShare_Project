namespace Backend.Features.Users.Dtos;

public record LoginUserResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    bool EmailVerified // nou
);

