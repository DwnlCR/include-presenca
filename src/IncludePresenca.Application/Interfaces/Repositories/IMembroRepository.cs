using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IMembroRepository
{
    Task<Membro?> GetByIdAsync(Guid id);
    Task<Membro?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task AddAsync(Membro member);
}