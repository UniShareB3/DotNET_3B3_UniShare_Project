using System.Security.Cryptography;
using System.Text;

namespace Backend.Services.Hashing;

public class HashingService : IHashingService
{
    public string HashCode(string code)
    {
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
