using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(Member member);
}