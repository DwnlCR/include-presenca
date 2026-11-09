using IncludePresenca.Application.DTOs.Frequency;

namespace IncludePresenca.Application.Interfaces.Queries;

public interface IFrequencyQueries
{
    /// <summary>
    /// The caller must supply the authenticated member ID from a trusted identity context.
    /// Returns null if the member does not exist; an existing member can have a null percentage.
    /// </summary>
    Task<MemberFrequencySummaryResponse?> GetMemberFrequencyAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an empty list when no classified participations are found.
    /// Member existence and access authorization are the caller's responsibility.
    /// </summary>
    Task<IReadOnlyList<ParticipationHistoryItemResponse>> GetMemberHistoryAsync(
        Guid memberId, CancellationToken cancellationToken = default);
}
