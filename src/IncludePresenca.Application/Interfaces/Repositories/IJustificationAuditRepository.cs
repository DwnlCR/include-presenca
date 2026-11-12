using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IJustificationAuditRepository
{
    Task AddAsync(JustificationAudit audit, CancellationToken cancellationToken = default);
}
