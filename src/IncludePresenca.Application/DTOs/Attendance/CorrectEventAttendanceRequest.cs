using System.ComponentModel.DataAnnotations;

namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Administrative correction of a finalized event attendance.</summary>
public sealed record CorrectEventAttendanceRequest
{
    /// <summary>Target attendance status: Present, JustifiedAbsence, or UnjustifiedAbsence.</summary>
    [Required]
    public required string Status { get; init; }

    /// <summary>Reason recorded for the administrative correction.</summary>
    [Required]
    public required string AdministrativeReason { get; init; }

    /// <summary>Real UTC check-in time required when the target status is Present.</summary>
    public DateTimeOffset? CheckedInAtUtc { get; init; }

    /// <summary>Absence justification required when the target status is JustifiedAbsence.</summary>
    public string? AbsenceJustification { get; init; }

    /// <summary>Explicit UTC time of the absence justification.</summary>
    public DateTimeOffset? JustifiedAtUtc { get; init; }
}
