using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Attendance;

public sealed class CorrectEventAttendanceUseCase
{
    private const string AuditUnavailableMessage =
        "Administrative attendance correction cannot be persisted before audit support is available.";

    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;

    public CorrectEventAttendanceUseCase(
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository)
    {
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
    }

    public async Task ExecuteAsync(
        Guid eventId,
        Guid memberId,
        Guid directorId,
        EventParticipationStatus newStatus,
        string administrativeReason,
        DateTime? checkedInAtUtc = null,
        string? absenceJustification = null,
        DateTime? justifiedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.IsCancelled)
            throw new InvalidOperationException("Cannot correct attendance for a cancelled event.");

        EventParticipation participation = await _participationRepository
            .GetByEventAndMemberIdAsync(
                eventId,
                memberId,
                forUpdate: false,
                cancellationToken)
            ?? throw new KeyNotFoundException("Participation not found.");

        ValidateCorrection(
            eventEntity,
            participation,
            directorId,
            newStatus,
            administrativeReason,
            checkedInAtUtc,
            absenceJustification,
            justifiedAtUtc);

        throw new InvalidOperationException(AuditUnavailableMessage);
    }

    private static void ValidateCorrection(
        Event eventEntity,
        EventParticipation participation,
        Guid directorId,
        EventParticipationStatus newStatus,
        string administrativeReason,
        DateTime? checkedInAtUtc,
        string? absenceJustification,
        DateTime? justifiedAtUtc)
    {
        if (directorId == Guid.Empty)
            throw new UnauthorizedAccessException("Responsible director is required.");

        if (string.IsNullOrWhiteSpace(administrativeReason))
            throw new ArgumentException(
                "Administrative reason is required.",
                nameof(administrativeReason));

        if (participation.Status == EventParticipationStatus.Pending)
            throw new InvalidOperationException("Only finalized participation can be corrected.");

        if (!Enum.IsDefined(newStatus) || newStatus == EventParticipationStatus.Pending)
            throw new ArgumentException(
                "Only a final attendance status can be assigned.",
                nameof(newStatus));

        DateTime? correctedCheckedInAtUtc = null;
        string? correctedJustification = null;
        DateTime? correctedJustifiedAtUtc = null;

        switch (newStatus)
        {
            case EventParticipationStatus.Present:
                if (checkedInAtUtc is null)
                    throw new ArgumentException(
                        "A real UTC check-in time is required.",
                        nameof(checkedInAtUtc));

                if (checkedInAtUtc.Value.Kind != DateTimeKind.Utc)
                    throw new ArgumentException(
                        "Date must be in UTC.",
                        nameof(checkedInAtUtc));

                if (checkedInAtUtc < eventEntity.StartsAt ||
                    checkedInAtUtc >= eventEntity.EndsAt)
                {
                    throw new ArgumentException(
                        "Check-in time must be within the event period.",
                        nameof(checkedInAtUtc));
                }

                correctedCheckedInAtUtc = checkedInAtUtc;
                break;

            case EventParticipationStatus.JustifiedAbsence:
                if (string.IsNullOrWhiteSpace(absenceJustification))
                    throw new ArgumentException(
                        "Absence justification is required.",
                        nameof(absenceJustification));

                if (justifiedAtUtc is null)
                    throw new ArgumentException(
                        "A UTC justification time is required.",
                        nameof(justifiedAtUtc));

                if (justifiedAtUtc.Value.Kind != DateTimeKind.Utc)
                    throw new ArgumentException(
                        "Date must be in UTC.",
                        nameof(justifiedAtUtc));

                correctedJustification = absenceJustification.Trim();
                correctedJustifiedAtUtc = justifiedAtUtc;
                break;

            case EventParticipationStatus.UnjustifiedAbsence:
                break;
        }

        if (participation.Status == newStatus &&
            participation.CheckedInAtUtc == correctedCheckedInAtUtc &&
            participation.Justification == correctedJustification &&
            participation.JustifiedAtUtc == correctedJustifiedAtUtc)
        {
            throw new InvalidOperationException(
                "Attendance correction does not change participation.");
        }
    }
}
