namespace IncludePresenca.Application.DTOs.Events;

public record CreateEventRequest(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);