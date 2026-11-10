using IncludePresenca.Application.DTOs.Reports;

namespace IncludePresenca.Application.Interfaces.Queries;

/// <summary>
/// Report access must be authorized by the caller before querying.
/// </summary>
public interface IFrequencyReportQueries
{
    Task<GeneralFrequencyReportResponse> GetGeneralReportAsync(
        CancellationToken cancellationToken = default);

    Task<MemberFrequencyReportResponse> GetMemberReportAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns null if the specified member does not exist.
    /// </summary>
    Task<MemberFrequencyDetailResponse?> GetMemberDetailAsync(
        Guid memberId, CancellationToken cancellationToken = default);
}
