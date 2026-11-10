using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Xunit;

namespace IncludePresenca.Application.Tests.Domain;

public class EventParticipationTests
{
    private static readonly Guid EventId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Constructor_StartsAsPending()
    {
        EventParticipation participation = CreateParticipation();

        Assert.Equal(EventParticipationStatus.Pending, participation.Status);
        Assert.Null(participation.CheckedInAtUtc);
        Assert.Null(participation.Justification);
        Assert.Null(participation.JustifiedAtUtc);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_RejectsEmptyIdentifiers(bool emptyEventId, bool emptyMemberId)
    {
        Guid eventId = emptyEventId ? Guid.Empty : EventId;
        Guid memberId = emptyMemberId ? Guid.Empty : MemberId;

        Assert.Throws<ArgumentException>(() => new EventParticipation(eventId, memberId));
    }

    [Fact]
    public void RegisterPresence_StoresUtcCheckInAndChangesStatus()
    {
        EventParticipation participation = CreateParticipation();
        DateTime checkedInAtUtc = new(2026, 11, 9, 12, 10, 0, DateTimeKind.Utc);

        participation.RegisterPresence(checkedInAtUtc);

        Assert.Equal(EventParticipationStatus.Present, participation.Status);
        Assert.Equal(checkedInAtUtc, participation.CheckedInAtUtc);
    }

    [Fact]
    public void RegisterPresence_RejectsNonUtcTime()
    {
        EventParticipation participation = CreateParticipation();
        DateTime localTime = new(2026, 11, 9, 12, 10, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => participation.RegisterPresence(localTime));
        Assert.Equal(EventParticipationStatus.Pending, participation.Status);
    }

    [Fact]
    public void JustifyAbsence_TrimsAndStoresJustificationData()
    {
        EventParticipation participation = CreateParticipation();
        DateTime justifiedAtUtc = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);

        participation.JustifyAbsence("  Consulta médica  ", justifiedAtUtc);

        Assert.Equal(EventParticipationStatus.JustifiedAbsence, participation.Status);
        Assert.Equal("Consulta médica", participation.Justification);
        Assert.Equal(justifiedAtUtc, participation.JustifiedAtUtc);
        Assert.Null(participation.CheckedInAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JustifyAbsence_RejectsMissingJustification(string? justification)
    {
        EventParticipation participation = CreateParticipation();
        DateTime justifiedAtUtc = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => participation.JustifyAbsence(justification!, justifiedAtUtc));
        Assert.Equal(EventParticipationStatus.Pending, participation.Status);
    }

    [Fact]
    public void JustifyAbsence_RejectsNonUtcTime()
    {
        EventParticipation participation = CreateParticipation();
        DateTime localTime = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => participation.JustifyAbsence("Consulta médica", localTime));
        Assert.Equal(EventParticipationStatus.Pending, participation.Status);
    }

    [Fact]
    public void ConsolidateUnjustifiedAbsence_FinalizesPendingParticipation()
    {
        EventParticipation participation = CreateParticipation();

        participation.ConsolidateUnjustifiedAbsence();

        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, participation.Status);
        Assert.Null(participation.CheckedInAtUtc);
        Assert.Null(participation.Justification);
    }

    [Theory]
    [InlineData(EventParticipationStatus.Present)]
    [InlineData(EventParticipationStatus.JustifiedAbsence)]
    [InlineData(EventParticipationStatus.UnjustifiedAbsence)]
    public void FinalizedParticipation_RejectsFurtherChanges(EventParticipationStatus finalStatus)
    {
        EventParticipation participation = CreateParticipation();
        DateTime utcNow = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);

        switch (finalStatus)
        {
            case EventParticipationStatus.Present:
                participation.RegisterPresence(utcNow);
                break;
            case EventParticipationStatus.JustifiedAbsence:
                participation.JustifyAbsence("Consulta médica", utcNow);
                break;
            case EventParticipationStatus.UnjustifiedAbsence:
                participation.ConsolidateUnjustifiedAbsence();
                break;
        }

        Assert.Throws<InvalidOperationException>(() => participation.RegisterPresence(utcNow));
        Assert.Throws<InvalidOperationException>(() => participation.JustifyAbsence("Outra justificativa", utcNow));
        Assert.Throws<InvalidOperationException>(() => participation.ConsolidateUnjustifiedAbsence());
        Assert.Equal(finalStatus, participation.Status);
    }

    private static EventParticipation CreateParticipation()
    {
        return new EventParticipation(EventId, MemberId);
    }
}
