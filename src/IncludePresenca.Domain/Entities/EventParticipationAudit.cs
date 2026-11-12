using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class EventParticipationAudit
{
    public Guid Id { get; private set; }
    public Guid ParticipationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid ChangedByMemberId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public EventParticipationStatus PreviousStatus { get; private set; }
    public EventParticipationStatus NewStatus { get; private set; }
    public DateTime? PreviousCheckedInAtUtc { get; private set; }
    public DateTime? NewCheckedInAtUtc { get; private set; }
    public string? PreviousJustification { get; private set; }
    public string? NewJustification { get; private set; }
    public DateTime? PreviousJustifiedAtUtc { get; private set; }
    public DateTime? NewJustifiedAtUtc { get; private set; }
    public string Reason { get; private set; }

    public EventParticipation Participation { get; private set; } = null!;
    public Event Event { get; private set; } = null!;
    public Member Member { get; private set; } = null!;
    public Member ChangedByMember { get; private set; } = null!;

    private EventParticipationAudit()
    {
        Reason = null!;
    }

    public EventParticipationAudit(
        Guid participationId,
        Guid eventId,
        Guid memberId,
        Guid changedByMemberId,
        DateTime changedAtUtc,
        EventParticipationStatus previousStatus,
        EventParticipationStatus newStatus,
        DateTime? previousCheckedInAtUtc,
        DateTime? newCheckedInAtUtc,
        string? previousJustification,
        string? newJustification,
        DateTime? previousJustifiedAtUtc,
        DateTime? newJustifiedAtUtc,
        string reason)
    {
        ValidateIdentifiers(participationId, eventId, memberId, changedByMemberId);
        EnsureUtc(changedAtUtc, nameof(changedAtUtc));
        EnsureOptionalUtc(previousCheckedInAtUtc, nameof(previousCheckedInAtUtc));
        EnsureOptionalUtc(newCheckedInAtUtc, nameof(newCheckedInAtUtc));
        EnsureOptionalUtc(previousJustifiedAtUtc, nameof(previousJustifiedAtUtc));
        EnsureOptionalUtc(newJustifiedAtUtc, nameof(newJustifiedAtUtc));

        if (!Enum.IsDefined(previousStatus))
            throw new ArgumentException("Previous status is invalid.", nameof(previousStatus));

        if (!Enum.IsDefined(newStatus))
            throw new ArgumentException("New status is invalid.", nameof(newStatus));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Audit reason is required.", nameof(reason));

        Id = Guid.NewGuid();
        ParticipationId = participationId;
        EventId = eventId;
        MemberId = memberId;
        ChangedByMemberId = changedByMemberId;
        ChangedAtUtc = changedAtUtc;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        PreviousCheckedInAtUtc = previousCheckedInAtUtc;
        NewCheckedInAtUtc = newCheckedInAtUtc;
        PreviousJustification = previousJustification;
        NewJustification = newJustification;
        PreviousJustifiedAtUtc = previousJustifiedAtUtc;
        NewJustifiedAtUtc = newJustifiedAtUtc;
        Reason = reason.Trim();
    }

    private static void ValidateIdentifiers(
        Guid participationId,
        Guid eventId,
        Guid memberId,
        Guid changedByMemberId)
    {
        if (participationId == Guid.Empty)
            throw new ArgumentException("Participation ID is required.", nameof(participationId));

        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID is required.", nameof(eventId));

        if (memberId == Guid.Empty)
            throw new ArgumentException("Member ID is required.", nameof(memberId));

        if (changedByMemberId == Guid.Empty)
            throw new ArgumentException(
                "Changed by member ID is required.",
                nameof(changedByMemberId));
    }

    private static void EnsureOptionalUtc(DateTime? value, string parameterName)
    {
        if (value is not null)
            EnsureUtc(value.Value, parameterName);
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date must be in UTC.", parameterName);
    }
}
