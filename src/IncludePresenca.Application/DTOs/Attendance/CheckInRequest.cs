namespace IncludePresenca.Application.DTOs.Attendance;

/// <summary>Secret code submitted by the authenticated member.</summary>
public sealed record CheckInRequest(string? Codigo, string? Code)
{
    public string? Value => Codigo ?? Code;
}
