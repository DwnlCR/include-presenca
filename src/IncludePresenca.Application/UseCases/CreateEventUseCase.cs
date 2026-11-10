
using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Events;

public class CreateEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IEventParticipationRepository _participationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventUseCase(
        IEventRepository eventRepository,
        IMemberRepository memberRepository,
        IEventParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _memberRepository = memberRepository;
        _participationRepository = participationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventResponse> ExecuteAsync(CreateEventRequest request)
    {
        Event eventEntity = new Event(
            request.Title,
            request.Description,
            request.StartsAt,
            request.EndsAt
        );

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _eventRepository.AddAsync(eventEntity);

            List<Member> members = await _memberRepository.GetActiveMembersAsync();

            List<EventParticipation> participations = members
                .Select(member => new EventParticipation(eventEntity.Id, member.Id))
                .ToList();

            await _participationRepository.AddRangeAsync(participations);
            await _unitOfWork.SaveChangesAsync();
        });

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
