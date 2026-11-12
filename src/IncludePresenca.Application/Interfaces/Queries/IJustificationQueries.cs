using IncludePresenca.Application.DTOs.Justifications;

namespace IncludePresenca.Application.Interfaces.Queries;

public interface IJustificationQueries
{
    Task<IReadOnlyList<PendingAbsenceResponse>> GetPendingAbsencesAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyJustificationResponse>> GetMineAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JustificationQueueItemResponse>> GetQueueAsync(CancellationToken cancellationToken = default);
}
