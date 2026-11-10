using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class EventCheckInCodeRepository : IEventCheckInCodeRepository
{
    private readonly AppDbContext _context;

    public EventCheckInCodeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task LockEventForCodeGenerationAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT "Id"
            FROM "Eventos"
            WHERE "Id" = {eventId}
            FOR UPDATE
            """, cancellationToken);
    }

    public async Task<EventCheckInCode?> GetByEventIdAsync(
        Guid eventId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default)
    {
        if (forUpdate)
        {
            return await _context.EventCheckInCodes
                .FromSqlInterpolated($"""
                SELECT *
                FROM "CodigosCheckin"
                WHERE "EventId" = {eventId}
                FOR UPDATE
                """)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await _context.EventCheckInCodes.SingleOrDefaultAsync(
            code => code.EventId == eventId,
            cancellationToken);
    }

    public async Task AddAsync(EventCheckInCode code, CancellationToken cancellationToken = default)
    {
        await _context.EventCheckInCodes.AddAsync(code, cancellationToken);
    }
}
