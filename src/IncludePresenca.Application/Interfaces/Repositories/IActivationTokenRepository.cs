
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IActivationTokenRepository
{
    Task<ActivationToken?> GetByTokenHashAsync(string tokenHash);
    Task<ActivationToken?> GetLatestByMemberIdAsync(Guid memberId);
    Task<List<ActivationToken>> GetActiveByMemberIdAsync(Guid memberId);
    Task AddAsync(ActivationToken token);
}
