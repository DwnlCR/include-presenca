namespace IncludePresenca.Application.DTOs.Events;

public record UpdateEventRequest(
    string Title,
    string? Description,
    DateTime StartsAt, 
    DateTime EndsAt
);