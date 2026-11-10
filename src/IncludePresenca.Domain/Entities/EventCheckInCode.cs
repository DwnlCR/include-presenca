namespace IncludePresenca.Domain.Entities;

public class EventCheckInCode
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string CodeHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    public Event Event { get; private set; } = null!;

    private EventCheckInCode()
    {
        CodeHash = null!;
    }

    public EventCheckInCode(
        Guid eventId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        Validate(eventId, codeHash, createdAtUtc, expiresAtUtc);

        Id = Guid.NewGuid();
        EventId = eventId;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Renew(string codeHash, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        Validate(EventId, codeHash, createdAtUtc, expiresAtUtc);

        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsValid(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date must be in UTC.", nameof(nowUtc));

        return nowUtc < ExpiresAtUtc;
    }

    private static void Validate(
        Guid eventId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID is required.", nameof(eventId));

        if (string.IsNullOrWhiteSpace(codeHash))
            throw new ArgumentException("Code hash is required.", nameof(codeHash));

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Creation date must be in UTC.", nameof(createdAtUtc));

        if (expiresAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Expiration date must be in UTC.", nameof(expiresAtUtc));

        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Expiration must be after creation.", nameof(expiresAtUtc));
    }
}
