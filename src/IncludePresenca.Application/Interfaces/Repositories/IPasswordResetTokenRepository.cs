
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);

    Task<List<PasswordResetToken>> GetActiveByMemberIdAsync(Guid memberId);

    Task AddAsync(PasswordResetToken token);

    Task<PasswordResetToken?> GetLatestByMemberIdAsync(Guid memberId);

    Task<bool> TryConsumeAsync(Guid tokenId);
}
