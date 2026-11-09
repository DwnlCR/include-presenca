
namespace IncludePresenca.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public bool IsCancelled { get; private set; }

    private Event()
    {
        Title = null!;
    }

    public Event(
        string title,
        string? description,
        DateTime startsAt,
        DateTime endsAt)
    {
        Validate(title, startsAt, endsAt);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description?.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
        IsCancelled = false;
    }

    public void Update(
        string title,
        string? description,
        DateTime startsAt,
        DateTime endsAt)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Cannot update a cancelled event.");

        Validate(title, startsAt, endsAt);

        Title = title.Trim();
        Description = description?.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void Cancel()
    {
        if (IsCancelled)
            throw new InvalidOperationException("Event is already cancelled.");

        IsCancelled = true;
    }

    private static void Validate(
        string title,
        DateTime startsAt,
        DateTime endsAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (startsAt.Kind != DateTimeKind.Utc ||
            endsAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Event dates must be in UTC.");

        if (endsAt <= startsAt)
            throw new ArgumentException("End date must be after start date.");
    }
}
