using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class ActivationTokenRepository : IActivationTokenRepository
{
    private readonly AppDbContext _context;

    public ActivationTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ActivationToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.ActivationTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task AddAsync(ActivationToken token)
    {
        await _context.ActivationTokens.AddAsync(token);
    }
}