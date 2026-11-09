using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Identity;

public class UpdateMemberUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberUseCase(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberResponse> ExecuteAsync(Guid id, UpdateMemberRequest request)
    {
        Member member = await _memberRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Member not found.");

        string normalizedEmail = request.Email?.Trim().ToLowerInvariant()
            ?? throw new ArgumentException("Email is required.");

        if (member.Email != normalizedEmail &&
            await _memberRepository.ExistsByEmailAsync(normalizedEmail))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        if (member.Role == Role.Diretoria &&
            request.Role != Role.Diretoria &&
            member.AccountStatus == StatusConta.Ativa &&
            !await _memberRepository.ExistsOtherActiveDirectorAsync(member.Id))
        {
            throw new InvalidOperationException(
                "Cannot change the role of the last active director.");
        }

        member.Update(request.Name, normalizedEmail, request.Role);

        await _unitOfWork.SaveChangesAsync();

        return new MemberResponse(
            member.Id,
            member.Name,
            member.Email,
            member.Role,
            member.AccountStatus
        );
    }
}
    
