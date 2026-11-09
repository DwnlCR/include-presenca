namespace IncludePresenca.Application.DTOs.Frequency;

/// <summary>
/// EventDate semantics and lateness rules require alignment with the Events module.
/// </summary>
public record ParticipationHistoryItemResponse(
    Guid EventId,
    string EventName,
    DateTimeOffset EventDate,
    ParticipationStatus Status,
    bool IsLate);
