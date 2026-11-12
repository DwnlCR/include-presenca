using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.Interfaces.Repositories;

public interface IJustificationRequestRepository
{
    Task AddAsync(JustificationRequest request, CancellationToken cancellationToken = default);
    Task<JustificationRequest?> GetByIdAsync(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingOrApprovedAsync(Guid participationId, CancellationToken cancellationToken = default);
}
