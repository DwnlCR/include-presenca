using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IEventParticipationAuditRepository
{
    Task AddAsync(
        EventParticipationAudit audit,
        CancellationToken cancellationToken = default);
}
