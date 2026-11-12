using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class JustificationAudit
{
    public Guid Id { get; private set; }
    public Guid JustificationRequestId { get; private set; }
    public Guid ParticipationId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid ChangedByMemberId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public JustificationRequestStatus PreviousStatus { get; private set; }
    public JustificationRequestStatus NewStatus { get; private set; }
    public EventParticipationStatus? PreviousParticipationStatus { get; private set; }
    public EventParticipationStatus? NewParticipationStatus { get; private set; }
    public string Origin { get; private set; }

    public JustificationRequest Request { get; private set; } = null!;
    public EventParticipation Participation { get; private set; } = null!;
    public Member Member { get; private set; } = null!;
    public Member ChangedByMember { get; private set; } = null!;

    private JustificationAudit() { Origin = null!; }

    public JustificationAudit(
        JustificationRequest request,
        Guid changedByMemberId,
        DateTime changedAtUtc,
        JustificationRequestStatus previousStatus,
        EventParticipationStatus? previousParticipationStatus,
        EventParticipationStatus? newParticipationStatus)
    {
        if (request.Id == Guid.Empty)
            throw new ArgumentException("Justification request is required.", nameof(request));
        if (changedByMemberId == Guid.Empty)
            throw new ArgumentException("Responsible member is required.", nameof(changedByMemberId));
        if (!Enum.IsDefined(previousStatus))
            throw new ArgumentException("Previous status is invalid.", nameof(previousStatus));
        EnsureUtc(changedAtUtc);

        Id = Guid.NewGuid();
        JustificationRequestId = request.Id;
        ParticipationId = request.ParticipationId;
        MemberId = request.MemberId;
        ChangedByMemberId = changedByMemberId;
        ChangedAtUtc = changedAtUtc;
        PreviousStatus = previousStatus;
        NewStatus = request.Status;
        PreviousParticipationStatus = previousParticipationStatus;
        NewParticipationStatus = newParticipationStatus;
        Origin = "JustificationDecision";
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date must be in UTC.", nameof(value));
    }
}
