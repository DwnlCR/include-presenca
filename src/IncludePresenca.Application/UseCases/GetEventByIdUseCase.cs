using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Events;

public class GetEventByIdUseCase
{
    private readonly IEventRepository _eventRepository;

    public GetEventByIdUseCase(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventResponse> ExecuteAsync(Guid id)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Event not found.");

        return new EventResponse(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.StartsAt,
            eventEntity.EndsAt,
            eventEntity.IsCancelled
        );
    }
}