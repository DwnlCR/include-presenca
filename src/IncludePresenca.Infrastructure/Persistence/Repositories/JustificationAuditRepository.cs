using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Infrastructure.Persistence.Repositories;

public sealed class JustificationAuditRepository : IJustificationAuditRepository
{
    private readonly AppDbContext _context;
    public JustificationAuditRepository(AppDbContext context) => _context = context;
    public Task AddAsync(JustificationAudit audit, CancellationToken cancellationToken = default) =>
        _context.JustificationAudits.AddAsync(audit, cancellationToken).AsTask();
}
