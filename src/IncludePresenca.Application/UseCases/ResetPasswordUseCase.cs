
using IncludePresenca.Application.DTOs.Auth;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Auth;

public class ResetPasswordUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordUseCase(
        IMemberRepository memberRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException("Password reset token is required.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("Password must contain at least 8 characters.");

        string tokenHash = _tokenService.HashToken(request.Token);

        PasswordResetToken resetToken =
            await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash)
            ?? throw new ArgumentException("Invalid password reset token.");

        if (!resetToken.IsValid())
            throw new ArgumentException("Invalid or expired password reset token.");

        Member member = await _memberRepository.GetByIdAsync(resetToken.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (member.AccountStatus != StatusConta.Ativa)
            throw new ArgumentException("Account is not active.");

        string passwordHash = _passwordHasher.Hash(request.Password);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            bool consumed = await _passwordResetTokenRepository.TryConsumeAsync(
                resetToken.Id
            );

            if (!consumed)
                throw new ArgumentException("Invalid or expired password reset token.");

            member.ResetPassword(passwordHash);
        });
    }
}
