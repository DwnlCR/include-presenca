
namespace IncludePresenca.Domain.Entities;

public class EventParticipation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid MemberId { get; private set; }

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
    }
}
