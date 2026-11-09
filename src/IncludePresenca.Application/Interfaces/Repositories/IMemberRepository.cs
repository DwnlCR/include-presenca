using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IMemberRepository
{
    Task<List<Member>> GetAllAsync();
    Task<Member?> GetByIdAsync(Guid id);
    Task<Member?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task AddAsync(Member member);

    Task<bool> ExistsOtherActiveDirectorAsync(Guid memberId);
}