using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IEventCheckInCodeRepository
{
    Task LockEventForCodeGenerationAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<EventCheckInCode?> GetByEventIdAsync(
        Guid eventId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(EventCheckInCode code, CancellationToken cancellationToken = default);
}
