using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Events;

public class CancelEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelEventUseCase(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Event not found");

        eventEntity.Cancel();

        await _unitOfWork.SaveChangesAsync();
    }
}