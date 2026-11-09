using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Events;

public class GetEventsUseCase
{
    private readonly IEventRepository _eventRepository;

    public GetEventsUseCase(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<List<EventResponse>> ExecuteAsync()
    {
        List<Event> events = await _eventRepository.GetAllAsync();

        return events.Select(eventEntity => new EventResponse(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.StartsAt,
            eventEntity.EndsAt,
            eventEntity.IsCancelled
        )).ToList();
    }
}