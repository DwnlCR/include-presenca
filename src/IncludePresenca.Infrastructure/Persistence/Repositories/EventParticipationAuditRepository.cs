using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public class EventParticipationAuditRepository : IEventParticipationAuditRepository
{
    private readonly AppDbContext _context;

    public EventParticipationAuditRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        EventParticipationAudit audit,
        CancellationToken cancellationToken = default)
    {
        await _context.EventParticipationAudits.AddAsync(audit, cancellationToken);
    }
}
