using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public sealed class JustificationParticipationRepository : IJustificationParticipationRepository
{
    private readonly AppDbContext _context;
    public JustificationParticipationRepository(AppDbContext context) => _context = context;

    public async Task<EventParticipation?> GetByIdAsync(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default)
    {
        if (forUpdate)
        {
            return await _context.EventParticipations
                .FromSqlInterpolated($"SELECT * FROM \"ParticipacoesEventos\" WHERE \"Id\" = {id} FOR UPDATE")
                .Include(x => x.Event).Include(x => x.Member)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await _context.EventParticipations.Include(x => x.Event).Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
