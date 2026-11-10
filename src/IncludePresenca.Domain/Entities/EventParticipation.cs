
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class EventParticipation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid MemberId { get; private set; }
    public EventParticipationStatus Status { get; private set; }
    public DateTime? CheckedInAtUtc { get; private set; }
    public string? Justification { get; private set; }
    public DateTime? JustifiedAtUtc { get; private set; }

    public Event Event { get; private set; } = null!;
    public Member Member { get; private set; } = null!;

    private EventParticipation()
    {
    }

    public EventParticipation(Guid eventId, Guid memberId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID is required.", nameof(eventId));

        if (memberId == Guid.Empty)
            throw new ArgumentException("Member ID is required.", nameof(memberId));

        Id = Guid.NewGuid();
        EventId = eventId;
        MemberId = memberId;
        Status = EventParticipationStatus.Pending;
    }

    public void RegisterPresence(DateTime checkedInAtUtc)
    {
        EnsurePending();
        EnsureUtc(checkedInAtUtc, nameof(checkedInAtUtc));

        Status = EventParticipationStatus.Present;
        CheckedInAtUtc = checkedInAtUtc;
    }

    public void JustifyAbsence(string justification, DateTime justifiedAtUtc)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Justification is required.", nameof(justification));

        EnsureUtc(justifiedAtUtc, nameof(justifiedAtUtc));

        Status = EventParticipationStatus.JustifiedAbsence;
        Justification = justification.Trim();
        JustifiedAtUtc = justifiedAtUtc;
    }

    public void ConsolidateUnjustifiedAbsence()
    {
        EnsurePending();

        Status = EventParticipationStatus.UnjustifiedAbsence;
    }

    private void EnsurePending()
    {
        if (Status != EventParticipationStatus.Pending)
            throw new InvalidOperationException("Participation is already finalized.");
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date must be in UTC.", parameterName);
    }
}
