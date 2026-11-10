namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Manual attendance decision made by an authorized director.</summary>
public sealed record UpdateAttendanceRequest(
    string Status,
    string? Justification);
