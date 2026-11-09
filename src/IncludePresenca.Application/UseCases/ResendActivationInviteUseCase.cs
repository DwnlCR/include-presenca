
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Email;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Identity;

public class ResendActivationInviteUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IActivationTokenRepository _activationTokenRepository;
    private readonly IActivationTokenService _activationTokenService;
    private readonly IActivationEmailSender _activationEmailSender;
    private readonly IUnitOfWork _unitOfWork;

    public ResendActivationInviteUseCase(
        IMemberRepository memberRepository,
        IActivationTokenRepository activationTokenRepository,
        IActivationTokenService activationTokenService,
        IActivationEmailSender activationEmailSender,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _activationTokenRepository = activationTokenRepository;
        _activationTokenService = activationTokenService;
        _activationEmailSender = activationEmailSender;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid memberId)
    {
        Member member = await _memberRepository.GetByIdAsync(memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (member.AccountStatus != StatusConta.PendenteAtivacao)
        {
            throw new InvalidOperationException(
                "Member account is not pending activation."
            );
        }

        List<ActivationToken> activeTokens =
            await _activationTokenRepository.GetActiveByMemberIdAsync(memberId);

        foreach (ActivationToken activeToken in activeTokens)
        {
            activeToken.Invalidate();
        }

        string token = _activationTokenService.GenerateToken();
        string tokenHash = _activationTokenService.HashToken(token);

        ActivationToken activationToken = new ActivationToken(
            member.Id,
            tokenHash,
            DateTime.UtcNow.AddHours(24)
        );

        await _activationTokenRepository.AddAsync(activationToken);

        await _unitOfWork.SaveChangesAsync();

        await _activationEmailSender.SendActivationEmailAsync(
            member.Email,
            member.Name,
            token
        );
    }
}
