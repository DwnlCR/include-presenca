using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Identity;

public class GetMembersUseCase
{
    private readonly IMemberRepository _memberRepository;

    public GetMembersUseCase(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<List<MemberResponse>> ExecuteAsync()
    {
        List<Member> members = await _memberRepository.GetAllAsync();

        return members.Select(member => new MemberResponse(
            member.Id,
            member.Name,
            member.Email,
            member.Role,
            member.AccountStatus
        )).ToList();
    }
}