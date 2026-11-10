using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IEventParticipationRepository
{
    Task AddRangeAsync(List<EventParticipation> participations);
    Task<List<EventParticipation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventParticipation?> GetByEventAndMemberIdAsync(
        Guid eventId,
        Guid memberId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default);
}
