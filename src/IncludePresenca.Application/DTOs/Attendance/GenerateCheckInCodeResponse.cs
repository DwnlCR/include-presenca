namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Secret check-in code returned only to an authorized director.</summary>
public sealed record GenerateCheckInCodeResponse(
    string Codigo,
    DateTimeOffset ExpiraEmUtc);
