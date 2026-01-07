namespace Backend.Features.Users.DTO;

public record LoginUserResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);

