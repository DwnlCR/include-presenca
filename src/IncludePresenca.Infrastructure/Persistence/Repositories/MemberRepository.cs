using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _context;

    public MemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(Guid id)
    {
        return await _context.Members
            .FirstOrDefaultAsync(member => member.Id == id);
    }

    public async Task<Member?> GetByEmailAsync(string email)
    {
        return await _context.Members
            .FirstOrDefaultAsync(member => member.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Members
            .AnyAsync(member => member.Email == email);
    }

    public async Task AddAsync(Member member)
    {
        await _context.Members.AddAsync(member);
    }
}