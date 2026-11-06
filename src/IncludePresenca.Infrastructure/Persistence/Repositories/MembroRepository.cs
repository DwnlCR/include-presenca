using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class MembroRepository : IMembroRepository
{
    private readonly AppDbContext _context;

    public MembroRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Membro?> GetByIdAsync(Guid id)
    {
        return await _context.Membros
            .FirstOrDefaultAsync(member => member.Id == id);
    }

    public async Task<Membro?> GetByEmailAsync(string email)
    {
        return await _context.Membros
            .FirstOrDefaultAsync(member => member.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Membros
            .AnyAsync(member => member.Email == email);
    }

    public async Task AddAsync(Membro member)
    {
        await _context.Membros.AddAsync(member);
    }
}