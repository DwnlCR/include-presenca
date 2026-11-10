namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Attendance state for an eligible event member.</summary>
public sealed record EventAttendanceResponse(
    Guid MembroId,
    string NomeMembro,
    string EmailMembro,
    string Status,
    DateTimeOffset? CheckedInAtUtc,
    string? Justification,
    DateTimeOffset? JustifiedAtUtc,
    bool IsLate);
