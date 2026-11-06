using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Security;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Application.Exceptions;

namespace IncludePresenca.Application.UseCases.Identity;

public class CreateMemberUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IActivationTokenRepository _activationTokenRepository;
    private readonly IActivationTokenService _activationTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMemberUseCase(
        IMemberRepository memberRepository,
        IActivationTokenRepository activationTokenRepository,
        IActivationTokenService activationTokenService,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _activationTokenRepository = activationTokenRepository;
        _activationTokenService = activationTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateMemberResponse> ExecuteAsync(CreateMemberRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        if (await _memberRepository.ExistsByEmailAsync(email))
        {
            throw new EmailAlreadyExistsException();
        }

        Member member = new Member(
            request.Name,
            email,
            request.Role
        );

        string token = _activationTokenService.GenerateToken();
        string tokenHash = _activationTokenService.HashToken(token);

        ActivationToken activationToken = new ActivationToken(
            member.Id,
            tokenHash,
            DateTime.UtcNow.AddHours(24)
        );

        await _memberRepository.AddAsync(member);
        await _activationTokenRepository.AddAsync(activationToken);

        await _unitOfWork.SaveChangesAsync();

        return new CreateMemberResponse(member.Id, token);
    }
}