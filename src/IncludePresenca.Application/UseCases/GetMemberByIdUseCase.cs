using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Identity;

public class GetMemberByIdUseCase
{
    private readonly IMemberRepository _memberRepository;

    public GetMemberByIdUseCase(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberResponse> ExecuteAsync(Guid id)
    {
        Member member = await _memberRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Member not found");

        return new MemberResponse(member.Id, member.Name, member.Email, member.Role, member.AccountStatus);
    }
}