using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Events;

public class GetEventSummaryUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;

    public GetEventSummaryUseCase(
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository)
    {
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
    }

    public async Task<EventSummaryResponse> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        List<Event> events = await _eventRepository.GetAllAsync();

        List<Event> activeEvents = events
            .Where(eventEntity => !eventEntity.IsCancelled)
            .ToList();

        Event? nextEvent = activeEvents
            .Where(eventEntity => eventEntity.StartsAt > now)
            .OrderBy(eventEntity => eventEntity.StartsAt)
            .FirstOrDefault();

        NextEventResponse? nextEventResponse = nextEvent is null
            ? null
            : new NextEventResponse(
                nextEvent.Title,
                nextEvent.StartsAt);

        TimeZoneInfo brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Sao_Paulo");

        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(
            now,
            brazilTimeZone);

        int eventsThisMonth = activeEvents.Count(eventEntity =>
        {
            DateTime localStart = TimeZoneInfo.ConvertTimeFromUtc(
                eventEntity.StartsAt,
                brazilTimeZone);

            return localStart.Year == localNow.Year &&
                   localStart.Month == localNow.Month;
        });

        List<Guid> completedEventIds = activeEvents
            .Where(eventEntity => eventEntity.EndsAt <= now)
            .Select(eventEntity => eventEntity.Id)
            .ToList();

        decimal? averageAttendance = null;

        if (completedEventIds.Count > 0)
        {
            List<EventParticipation> participations =
                await _participationRepository.GetByEventIdsAsync(
                    completedEventIds,
                    cancellationToken);

            List<EventParticipation> finalizedParticipations = participations
                .Where(participation =>
                    participation.Status != EventParticipationStatus.Pending)
                .ToList();

            if (finalizedParticipations.Count > 0)
            {
                int presentCount = finalizedParticipations.Count(
                    participation =>
                        participation.Status == EventParticipationStatus.Present);

                averageAttendance = Math.Round(
                    (decimal)presentCount / finalizedParticipations.Count * 100,
                    2);
            }
        }

        return new EventSummaryResponse(
            nextEventResponse,
            eventsThisMonth,
            averageAttendance);
    }
}