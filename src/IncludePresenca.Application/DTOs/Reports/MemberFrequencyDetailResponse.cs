using IncludePresenca.Application.DTOs.Frequency;

namespace IncludePresenca.Application.DTOs.Reports;

public record MemberFrequencyDetailResponse(
    MemberFrequencySummaryResponse Summary,
    IReadOnlyList<ParticipationHistoryItemResponse> History);
