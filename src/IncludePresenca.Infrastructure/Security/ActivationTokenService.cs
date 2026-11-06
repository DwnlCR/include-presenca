using System.Security.Cryptography;
using System.Text;
using IncludePresenca.Application.Interfaces.Security;

namespace IncludePresenca.Infrastructure.Security;

public class ActivationTokenService : IActivationTokenService
{

    public string GenerateToken()
    {
        byte [] bytes = RandomNumberGenerator.GetBytes(32);

    return Convert.ToHexString(bytes);
    }

    public string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
    
}