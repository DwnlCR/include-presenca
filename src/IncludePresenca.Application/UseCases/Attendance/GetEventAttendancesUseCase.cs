using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Attendance;

public class GetEventAttendancesUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;

    public GetEventAttendancesUseCase(
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository)
    {
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
    }

    public async Task<List<EventAttendanceResponse>> ExecuteAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        List<EventParticipation> participations = await _participationRepository
            .GetByEventIdAsync(eventId, cancellationToken);

        return participations
            .Select(participation => new EventAttendanceResponse(
                participation.MemberId,
                participation.Member.Name,
                participation.Member.Email,
                participation.Status.ToString(),
                ToDateTimeOffset(participation.CheckedInAtUtc),
                participation.Justification,
                ToDateTimeOffset(participation.JustifiedAtUtc),
                IsLate(eventEntity, participation.CheckedInAtUtc)))
            .ToList();
    }

    internal static bool IsLate(Event eventEntity, DateTime? checkedInAtUtc)
    {
        return checkedInAtUtc is not null &&
            checkedInAtUtc.Value > eventEntity.StartsAt.AddMinutes(10);
    }

    internal static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(value.Value);
    }
}
