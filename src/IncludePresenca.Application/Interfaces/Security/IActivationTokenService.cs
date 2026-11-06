namespace IncludePresenca.Application.Interfaces.Security;

public interface IActivationTokenService
{
    string GenerateToken();
    string HashToken(string token);
}