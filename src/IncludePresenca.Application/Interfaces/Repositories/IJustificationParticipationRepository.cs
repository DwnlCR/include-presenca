using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IJustificationParticipationRepository
{
    Task<EventParticipation?> GetByIdAsync(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default);
}
