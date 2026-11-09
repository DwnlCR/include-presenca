using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Events;

public class UpdateEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventUseCase(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventResponse> ExecuteAsync(Guid id, UpdateEventRequest request)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Event not found.");

        eventEntity.Update(request.Title, request.Description, request.StartsAt, request.EndsAt);

        await _unitOfWork.SaveChangesAsync();

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
