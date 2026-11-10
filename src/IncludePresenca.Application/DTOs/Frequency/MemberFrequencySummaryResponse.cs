namespace IncludePresenca.Application.DTOs.Frequency;

public record MemberFrequencySummaryResponse(
    Guid MemberId,
    string MemberName,
    MemberFrequencyCounts Counts,
    decimal? FrequencyPercentage);
