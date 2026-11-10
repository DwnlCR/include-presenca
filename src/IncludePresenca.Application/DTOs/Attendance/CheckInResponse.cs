namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Result of a successful member check-in.</summary>
public sealed record CheckInResponse(
    Guid ParticipacaoId,
    string Status,
    DateTimeOffset CheckedInAtUtc,
    bool IsLate);
