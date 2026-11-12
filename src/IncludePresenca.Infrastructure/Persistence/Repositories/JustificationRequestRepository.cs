using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public sealed class JustificationRequestRepository : IJustificationRequestRepository
{
    private readonly AppDbContext _context;

    public JustificationRequestRepository(AppDbContext context) => _context = context;

    public Task AddAsync(JustificationRequest request, CancellationToken cancellationToken = default) =>
        _context.JustificationRequests.AddAsync(request, cancellationToken).AsTask();

    public async Task<JustificationRequest?> GetByIdAsync(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default)
    {
        if (forUpdate)
        {
            return await _context.JustificationRequests
                .FromSqlInterpolated($"SELECT * FROM \"SolicitacoesJustificativas\" WHERE \"Id\" = {id} FOR UPDATE")
                .Include(x => x.Participation).Include(x => x.Member)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await _context.JustificationRequests
            .Include(x => x.Participation).Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsPendingOrApprovedAsync(Guid participationId, CancellationToken cancellationToken = default) =>
        _context.JustificationRequests.AnyAsync(
            x => x.ParticipationId == participationId &&
                (x.Status == JustificationRequestStatus.Pending || x.Status == JustificationRequestStatus.Approved),
            cancellationToken);
}
