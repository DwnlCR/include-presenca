
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _context;

    public PasswordResetTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task<PasswordResetToken?> GetLatestByMemberIdAsync(Guid memberId)
    {
        return await _context.PasswordResetTokens
            .Where(token => token.MemberId == memberId)
            .OrderByDescending(token => token.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PasswordResetToken>> GetActiveByMemberIdAsync(Guid memberId)
    {
        return await _context.PasswordResetTokens
            .Where(token =>
                token.MemberId == memberId &&
                token.UsedAt == null &&
                token.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _context.PasswordResetTokens.AddAsync(token);
    }

    
    public async Task<bool> TryConsumeAsync(Guid tokenId)
    {
        DateTime now = DateTime.UtcNow;

        int affectedRows = await _context.PasswordResetTokens
            .Where(token =>
                token.Id == tokenId &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(token => token.UsedAt, now)
            );

        return affectedRows == 1;
    }
}
