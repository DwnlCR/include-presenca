using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IncludePresenca.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Member member)
    {
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured.");

        string issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer not configured.");

        string audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience not configured.");

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, member.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, member.Email),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        ];

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(key)
        );

        SigningCredentials credentials = new(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}