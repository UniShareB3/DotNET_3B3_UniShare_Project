using System.Security.Cryptography;
using System.Text;

namespace Backend.Services;

public class HashingService : IHashingService
{
    public string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
