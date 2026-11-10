using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Identity;

public class ReactivateMemberUseCase
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateMemberUseCase(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        Member member = await _memberRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Member not found");

        member.Reactivate();

        await _unitOfWork.SaveChangesAsync();
    }
}