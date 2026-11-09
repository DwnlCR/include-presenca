
namespace IncludePresenca.Application.Interfaces.Security;

public interface ITokenService
{
    string GenerateToken();
    string HashToken(string token);
}
