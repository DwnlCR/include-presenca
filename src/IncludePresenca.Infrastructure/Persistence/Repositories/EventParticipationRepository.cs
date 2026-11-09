using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class EventParticipationRepository : IEventParticipationRepository
{
    private readonly AppDbContext _context;

    public EventParticipationRepository(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }

    public async Task AddRangeAsync(List<EventParticipation> participations)
    {
        await _context.EventParticipations.AddRangeAsync(participations);
    }
}