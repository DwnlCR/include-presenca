using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Identity;

public class DeactivateMemberUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateMemberUseCase(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        Member member = await _memberRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Member not found");
        
        if (member.Role == Role.Diretoria &&
            member.AccountStatus == StatusConta.Ativa &&
            !await _memberRepository.ExistsOtherActiveDirectorAsync(member.Id))
        {
            throw new InvalidOperationException(
                "Cannot deactivate the last active director.");
        }

        member.Deactivate();

        await _unitOfWork.SaveChangesAsync();
    }

}