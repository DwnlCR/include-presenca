namespace IncludePresenca.Application.DTOs.Events;

public record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsCancelled
);