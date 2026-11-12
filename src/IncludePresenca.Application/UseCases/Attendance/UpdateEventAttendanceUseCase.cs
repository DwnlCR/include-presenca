using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Attendance;

public class UpdateEventAttendanceUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventAttendanceUseCase(
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventAttendanceResponse> ExecuteAsync(
        Guid eventId,
        Guid memberId,
        UpdateAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.IsCancelled)
            throw new InvalidOperationException("Cannot update attendance for a cancelled event.");

        if (!Enum.TryParse(request.Status, ignoreCase: true, out EventParticipationStatus status) ||
            status == EventParticipationStatus.Pending)
            throw new ArgumentException("Only a final attendance status can be assigned.", nameof(request));

        EventParticipation participation = await _participationRepository
            .GetByEventAndMemberIdAsync(eventId, memberId, forUpdate: true, cancellationToken)
            ?? throw new KeyNotFoundException("Participation not found.");

        if (participation.Status != EventParticipationStatus.Pending)
            throw new InvalidOperationException(
                "Only pending participation can be updated by the normal attendance flow.");

        DateTime nowUtc = DateTime.UtcNow;

        switch (status)
        {
            case EventParticipationStatus.Present:
                participation.RegisterPresence(nowUtc);
                break;
            case EventParticipationStatus.JustifiedAbsence:
                participation.JustifyAbsence(request.Justification ?? string.Empty, nowUtc);
                break;
            case EventParticipationStatus.UnjustifiedAbsence:
                participation.ConsolidateUnjustifiedAbsence();
                break;
            default:
                throw new ArgumentException("Unsupported attendance status.", nameof(request));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EventAttendanceResponse(
            participation.MemberId,
            participation.Member.Name,
            participation.Member.Email,
            participation.Status.ToString(),
            GetDateTimeOffset(participation.CheckedInAtUtc),
            participation.Justification,
            GetDateTimeOffset(participation.JustifiedAtUtc),
            participation.CheckedInAtUtc is not null &&
                participation.CheckedInAtUtc.Value > eventEntity.StartsAt.AddMinutes(10));
    }

    private static DateTimeOffset? GetDateTimeOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(value.Value);
    }
}
