using IncludePresenca.Application.DTOs.Frequency;

namespace IncludePresenca.Application.DTOs.Reports;

public record MemberFrequencyReportResponse(
    IReadOnlyList<MemberFrequencySummaryResponse> Members);
