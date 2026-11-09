namespace IncludePresenca.Application.DTOs.Reports;

/// <summary>
/// Global percentage is calculated from totals, never from the mean of member percentages.
/// Percentages are unrounded and null when presences plus unjustified absences is zero.
/// </summary>
public record GeneralFrequencyReportResponse(
    long TotalPresences,
    long TotalJustifiedAbsences,
    long TotalUnjustifiedAbsences,
    decimal? GlobalFrequencyPercentage);
