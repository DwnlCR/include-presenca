
using IncludePresenca.Application.DTOs.Auth;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Auth;

public class LoginUseCase
{
    private readonly IMemberRepository _membroRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUseCase(
        IMemberRepository membroRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService
    )
    {
        _membroRepository = membroRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request)
    {
        if (
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        string email = request.Email.Trim().ToLowerInvariant();

        Member? member = await _membroRepository.GetByEmailAsync(email);

        if (member is null ||
            member.AccountStatus != StatusConta.Ativa ||
            string.IsNullOrWhiteSpace(member.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!_passwordHasher.Verify(request.Password, member.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        string accessToken = _jwtTokenService.GenerateAccessToken(member);

        return new LoginResponse(accessToken);
    }
}
