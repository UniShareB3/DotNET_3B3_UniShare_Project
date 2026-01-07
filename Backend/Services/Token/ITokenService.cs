using Backend.Data;

namespace Backend.Services.Token;

public interface ITokenService
{
    string GenerateToken(User user, IList<string> roles);
    string GenerateTemporaryToken(User user);
    string GenerateRefreshToken();
    int GetAccessTokenExpirationInSeconds();
    DateTime GetRefreshTokenExpirationDate();
}