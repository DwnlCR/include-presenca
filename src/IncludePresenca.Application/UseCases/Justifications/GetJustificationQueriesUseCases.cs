using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces.Queries;

namespace IncludePresenca.Application.UseCases.Justifications;

public sealed class GetPendingAbsencesUseCase
{
    private readonly IJustificationQueries _queries;
    public GetPendingAbsencesUseCase(IJustificationQueries queries) => _queries = queries;
    public Task<IReadOnlyList<PendingAbsenceResponse>> ExecuteAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _queries.GetPendingAbsencesAsync(memberId, cancellationToken);
}

public sealed class GetMyJustificationsUseCase
{
    private readonly IJustificationQueries _queries;
    public GetMyJustificationsUseCase(IJustificationQueries queries) => _queries = queries;
    public Task<IReadOnlyList<MyJustificationResponse>> ExecuteAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _queries.GetMineAsync(memberId, cancellationToken);
}

public sealed class GetJustificationQueueUseCase
{
    private readonly IJustificationQueries _queries;
    public GetJustificationQueueUseCase(IJustificationQueries queries) => _queries = queries;
    public Task<IReadOnlyList<JustificationQueueItemResponse>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        _queries.GetQueueAsync(cancellationToken);
}
