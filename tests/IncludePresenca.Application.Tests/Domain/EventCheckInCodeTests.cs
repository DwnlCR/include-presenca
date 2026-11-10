using IncludePresenca.Domain.Entities;
using Xunit;

namespace IncludePresenca.Application.Tests.Domain;

public class EventCheckInCodeTests
{
    private static readonly Guid EventId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void IsValid_ReturnsTrueBeforeExpiration()
    {
        DateTime createdAtUtc = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        EventCheckInCode code = new(EventId, "hash", createdAtUtc, createdAtUtc.AddMinutes(15));

        Assert.True(code.IsValid(createdAtUtc.AddMinutes(14)));
    }

    [Fact]
    public void IsValid_ReturnsFalseAtExpiration()
    {
        DateTime createdAtUtc = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        EventCheckInCode code = new(EventId, "hash", createdAtUtc, createdAtUtc.AddMinutes(15));

        Assert.False(code.IsValid(createdAtUtc.AddMinutes(15)));
    }

    [Fact]
    public void Renew_ReplacesHashAndValidityWindow()
    {
        DateTime createdAtUtc = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        EventCheckInCode code = new(EventId, "old-hash", createdAtUtc, createdAtUtc.AddMinutes(15));
        DateTime renewedAtUtc = createdAtUtc.AddMinutes(5);

        code.Renew("new-hash", renewedAtUtc, renewedAtUtc.AddMinutes(15));

        Assert.Equal("new-hash", code.CodeHash);
        Assert.Equal(renewedAtUtc, code.CreatedAtUtc);
        Assert.True(code.IsValid(renewedAtUtc.AddMinutes(14)));
    }

    [Fact]
    public void Constructor_RejectsNonUtcDates()
    {
        DateTime localDate = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Local);
        DateTime utcDate = new(2026, 11, 10, 12, 15, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => new EventCheckInCode(EventId, "hash", localDate, utcDate));
    }
}
