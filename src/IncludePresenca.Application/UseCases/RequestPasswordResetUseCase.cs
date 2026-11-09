
using IncludePresenca.Application.DTOs.Auth;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Email;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Auth;

public class RequestPasswordResetUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;
    private readonly IUnitOfWork _unitOfWork;

    public RequestPasswordResetUseCase(
        IMemberRepository memberRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ITokenService tokenService,
        IPasswordResetEmailSender passwordResetEmailSender,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenService = tokenService;
        _passwordResetEmailSender = passwordResetEmailSender;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(RequestPasswordResetRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        Member? member = await _memberRepository.GetByEmailAsync(email);

        if (member is null || member.AccountStatus != StatusConta.Ativa)
            return;

        PasswordResetToken? latestToken =
            await _passwordResetTokenRepository.GetLatestByMemberIdAsync(member.Id);

        if (latestToken is not null &&
            DateTime.UtcNow - latestToken.CreatedAt < TimeSpan.FromSeconds(60))
            return;

        string token = _tokenService.GenerateToken();
        string tokenHash = _tokenService.HashToken(token);

        PasswordResetToken passwordResetToken = new PasswordResetToken(
            member.Id,
            tokenHash,
            DateTime.UtcNow.AddMinutes(30)
        );

        await _passwordResetEmailSender.SendPasswordResetEmailAsync(
            member.Email,
            member.Name,
            token
        );

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            List<PasswordResetToken> activeTokens =
                await _passwordResetTokenRepository.GetActiveByMemberIdAsync(member.Id);

            foreach (PasswordResetToken activeToken in activeTokens)
                activeToken.Invalidate();

            await _passwordResetTokenRepository.AddAsync(passwordResetToken);
        });
    }
}
