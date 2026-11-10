using System.Text.Json.Serialization;

namespace IncludePresenca.Application.DTOs.Reports;

/// <summary>
/// Global percentage is calculated from totals, never from the mean of member percentages.
/// Percentages are unrounded and null when presences plus unjustified absences is zero.
/// </summary>
public record GeneralFrequencyReportResponse(
    long TotalPresences,
    long TotalJustifiedAbsences,
    long TotalUnjustifiedAbsences,
    decimal? GlobalFrequencyPercentage)
{
    /// <summary>Alias used by the director panel without removing the existing property.</summary>
    [JsonPropertyName("frequenciaGlobal")]
    public decimal? FrequenciaGlobal => GlobalFrequencyPercentage;

    /// <summary>Number of active members at the time of report generation.</summary>
    [JsonPropertyName("membrosAtivos")]
    public int MembrosAtivos { get; init; }

    /// <summary>Active members below the director panel's 75 percent threshold.</summary>
    [JsonPropertyName("abaixoDaMeta")]
    public int AbaixoDaMeta { get; init; }

    /// <summary>Member indicators derived from the same classified participations.</summary>
    [JsonPropertyName("membros")]
    public IReadOnlyList<DirectorMemberFrequencyResponse> Membros { get; init; } = [];
}
