using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Xunit;

namespace IncludePresenca.Application.Tests.Domain;

public sealed class EventParticipationAuditTests
{
    private static readonly Guid ParticipationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EventId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MemberId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ChangedByMemberId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTime ChangedAtUtc =
        new(2026, 11, 11, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_StoresValidAuditDataAndTrimsReason()
    {
        DateTime previousCheckedInAtUtc = ChangedAtUtc.AddHours(-1);
        DateTime newJustifiedAtUtc = ChangedAtUtc.AddMinutes(-10);

        EventParticipationAudit audit = new(
            ParticipationId,
            EventId,
            MemberId,
            ChangedByMemberId,
            ChangedAtUtc,
            EventParticipationStatus.Present,
            EventParticipationStatus.JustifiedAbsence,
            previousCheckedInAtUtc,
            null,
            null,
            "Consulta médica",
            null,
            newJustifiedAtUtc,
            "  Correção administrativa  ");

        Assert.NotEqual(Guid.Empty, audit.Id);
        Assert.Equal(ParticipationId, audit.ParticipationId);
        Assert.Equal(EventId, audit.EventId);
        Assert.Equal(MemberId, audit.MemberId);
        Assert.Equal(ChangedByMemberId, audit.ChangedByMemberId);
        Assert.Equal(ChangedAtUtc, audit.ChangedAtUtc);
        Assert.Equal(EventParticipationStatus.Present, audit.PreviousStatus);
        Assert.Equal(EventParticipationStatus.JustifiedAbsence, audit.NewStatus);
        Assert.Equal(previousCheckedInAtUtc, audit.PreviousCheckedInAtUtc);
        Assert.Equal("Consulta médica", audit.NewJustification);
        Assert.Equal(newJustifiedAtUtc, audit.NewJustifiedAtUtc);
        Assert.Equal("Correção administrativa", audit.Reason);
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void Constructor_RejectsEmptyIdentifiers(
        bool emptyParticipationId,
        bool emptyEventId,
        bool emptyMemberId,
        bool emptyChangedByMemberId)
    {
        Assert.Throws<ArgumentException>(() => new EventParticipationAudit(
            emptyParticipationId ? Guid.Empty : ParticipationId,
            emptyEventId ? Guid.Empty : EventId,
            emptyMemberId ? Guid.Empty : MemberId,
            emptyChangedByMemberId ? Guid.Empty : ChangedByMemberId,
            ChangedAtUtc,
            EventParticipationStatus.Present,
            EventParticipationStatus.JustifiedAbsence,
            null,
            null,
            null,
            "Consulta médica",
            null,
            ChangedAtUtc,
            "Correção administrativa"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingReason(string? reason)
    {
        Assert.Throws<ArgumentException>(() => new EventParticipationAudit(
            ParticipationId,
            EventId,
            MemberId,
            ChangedByMemberId,
            ChangedAtUtc,
            EventParticipationStatus.Present,
            EventParticipationStatus.JustifiedAbsence,
            null,
            null,
            null,
            "Consulta médica",
            null,
            ChangedAtUtc,
            reason!));
    }

    [Theory]
    [MemberData(nameof(NonUtcDates))]
    public void Constructor_RejectsNonUtcTimestamps(
        DateTime changedAtUtc,
        DateTime? previousCheckedInAtUtc,
        DateTime? newCheckedInAtUtc,
        DateTime? previousJustifiedAtUtc,
        DateTime? newJustifiedAtUtc)
    {
        Assert.Throws<ArgumentException>(() => new EventParticipationAudit(
            ParticipationId,
            EventId,
            MemberId,
            ChangedByMemberId,
            changedAtUtc,
            EventParticipationStatus.Present,
            EventParticipationStatus.JustifiedAbsence,
            previousCheckedInAtUtc,
            newCheckedInAtUtc,
            null,
            "Consulta médica",
            previousJustifiedAtUtc,
            newJustifiedAtUtc,
            "Correção administrativa"));
    }

    public static IEnumerable<object?[]> NonUtcDates()
    {
        DateTime local = new(2026, 11, 11, 14, 0, 0, DateTimeKind.Local);
        DateTime unspecified = new(2026, 11, 11, 14, 0, 0, DateTimeKind.Unspecified);
        DateTime utc = DateTime.SpecifyKind(local, DateTimeKind.Utc);

        yield return [local, null, null, null, null];
        yield return [utc, local, null, null, null];
        yield return [utc, null, unspecified, null, null];
        yield return [utc, null, null, local, null];
        yield return [utc, null, null, null, unspecified];
    }
}
