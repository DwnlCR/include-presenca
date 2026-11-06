using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Identity;

public class ActivateMemberUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IActivationTokenRepository _activationTokenRepository;
    private readonly IActivationTokenService _activationTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateMemberUseCase(
        IMemberRepository memberRepository,
        IActivationTokenRepository activationTokenRepository,
        IActivationTokenService activationTokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _activationTokenRepository = activationTokenRepository;
        _activationTokenService = activationTokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(ActivateMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Activation token is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ArgumentException("Password must contain at least 8 characters.");
        }

        string tokenHash = _activationTokenService.HashToken(request.Token);

        ActivationToken activationToken =
            await _activationTokenRepository.GetByTokenHashAsync(tokenHash)
            ?? throw new ArgumentException("Invalid activation token.");

        if (!activationToken.IsValid())
        {
            throw new ArgumentException("Invalid or expired activation token.");
        }

        Member member = await _memberRepository.GetByIdAsync(activationToken.MemberId)
            ?? throw new KeyNotFoundException("Member not found.");

        string passwordHash = _passwordHasher.Hash(request.Password);

        member.ActivateAccount(passwordHash);
        activationToken.Consume();

        await _unitOfWork.SaveChangesAsync();
    }
}