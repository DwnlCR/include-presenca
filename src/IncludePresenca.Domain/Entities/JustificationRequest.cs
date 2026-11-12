using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class JustificationRequest
{
    public Guid Id { get; private set; }
    public Guid ParticipationId { get; private set; }
    public Guid MemberId { get; private set; }
    public JustificationReason Reason { get; private set; }
    public string Description { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime DeadlineUtc { get; private set; }
    public JustificationRequestStatus Status { get; private set; }
    public Guid? DecidedByMemberId { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public string? AttachmentStorageKey { get; private set; }
    public string? AttachmentOriginalFileName { get; private set; }
    public string? AttachmentContentType { get; private set; }
    public long? AttachmentLength { get; private set; }

    public EventParticipation Participation { get; private set; } = null!;
    public Member Member { get; private set; } = null!;
    public Member? DecidedByMember { get; private set; }

    private JustificationRequest()
    {
        Description = null!;
    }

    public JustificationRequest(
        Guid participationId,
        Guid memberId,
        JustificationReason reason,
        string description,
        DateTime submittedAtUtc,
        DateTime deadlineUtc,
        string? attachmentStorageKey = null,
        string? attachmentOriginalFileName = null,
        string? attachmentContentType = null,
        long? attachmentLength = null)
    {
        ValidateIdentifiers(participationId, memberId);
        ValidateText(description);
        EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        EnsureUtc(deadlineUtc, nameof(deadlineUtc));

        if (!Enum.IsDefined(reason))
            throw new ArgumentException("Justification reason is invalid.", nameof(reason));

        if (deadlineUtc <= submittedAtUtc)
            throw new ArgumentException("Deadline must be after submission.", nameof(deadlineUtc));

        if (attachmentLength is < 1)
            throw new ArgumentException("Attachment must not be empty.", nameof(attachmentLength));

        Id = Guid.NewGuid();
        ParticipationId = participationId;
        MemberId = memberId;
        Reason = reason;
        Description = description.Trim();
        SubmittedAtUtc = submittedAtUtc;
        DeadlineUtc = deadlineUtc;
        Status = JustificationRequestStatus.Pending;
        AttachmentStorageKey = attachmentStorageKey;
        AttachmentOriginalFileName = attachmentOriginalFileName;
        AttachmentContentType = attachmentContentType;
        AttachmentLength = attachmentLength;
    }

    public void Approve(Guid directorId, DateTime decidedAtUtc)
    {
        EnsurePending();
        ValidateDecision(directorId, decidedAtUtc);
        Status = JustificationRequestStatus.Approved;
        DecidedByMemberId = directorId;
        DecidedAtUtc = decidedAtUtc;
    }

    public void Reject(Guid directorId, DateTime decidedAtUtc)
    {
        EnsurePending();
        ValidateDecision(directorId, decidedAtUtc);
        Status = JustificationRequestStatus.Rejected;
        DecidedByMemberId = directorId;
        DecidedAtUtc = decidedAtUtc;
    }

    private void EnsurePending()
    {
        if (Status != JustificationRequestStatus.Pending)
            throw new InvalidOperationException("Justification request was already decided.");
    }

    private static void ValidateDecision(Guid directorId, DateTime decidedAtUtc)
    {
        if (directorId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated director is required.");
        EnsureUtc(decidedAtUtc, nameof(decidedAtUtc));
    }

    private static void ValidateIdentifiers(Guid participationId, Guid memberId)
    {
        if (participationId == Guid.Empty)
            throw new ArgumentException("Participation ID is required.", nameof(participationId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("Member ID is required.", nameof(memberId));
    }

    private static void ValidateText(string description)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 50)
            throw new ArgumentException("Description must contain at least 50 characters.", nameof(description));
        if (description.Trim().Length > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date must be in UTC.", parameterName);
    }
}
