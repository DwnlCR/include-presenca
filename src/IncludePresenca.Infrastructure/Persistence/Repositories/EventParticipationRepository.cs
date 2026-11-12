using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<EventParticipation>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventParticipations
            .AsNoTracking()
            .Include(participation => participation.Member)
            .Where(participation => participation.EventId == eventId)
            .OrderBy(participation => participation.Member.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EventParticipation>> GetByEventIdsAsync(
        List<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventParticipations
            .AsNoTracking()
            .Where(participation => eventIds.Contains(participation.EventId))
            .ToListAsync(cancellationToken);
    }

    public async Task<EventParticipation?> GetByEventAndMemberIdAsync(
        Guid eventId,
        Guid memberId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default)
    {
        if (forUpdate)
        {
            return await _context.EventParticipations
                .FromSqlInterpolated($"""
                SELECT *
                FROM "ParticipacoesEventos"
                WHERE "EventId" = {eventId} AND "MemberId" = {memberId}
                FOR UPDATE
                """)
                .Include(participation => participation.Member)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await _context.EventParticipations
            .Include(participation => participation.Member)
            .SingleOrDefaultAsync(
                participation => participation.EventId == eventId && participation.MemberId == memberId,
                cancellationToken);
    }
}
