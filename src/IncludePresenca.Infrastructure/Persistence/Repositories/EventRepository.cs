using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetAllAsync()
    {
        return await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.StartsAt)
            .ToListAsync();
    }

    public async Task AddAsync(Event eventEntity)
    {
        await _context.Events.AddAsync(eventEntity);
    }
}