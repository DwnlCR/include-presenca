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

    [Theory]
    [InlineData(EventParticipationStatus.Present, EventParticipationStatus.JustifiedAbsence)]
    [InlineData(EventParticipationStatus.Present, EventParticipationStatus.UnjustifiedAbsence)]
    [InlineData(EventParticipationStatus.JustifiedAbsence, EventParticipationStatus.Present)]
    [InlineData(EventParticipationStatus.JustifiedAbsence, EventParticipationStatus.UnjustifiedAbsence)]
    [InlineData(EventParticipationStatus.UnjustifiedAbsence, EventParticipationStatus.Present)]
    [InlineData(EventParticipationStatus.UnjustifiedAbsence, EventParticipationStatus.JustifiedAbsence)]
    public void CorrectAttendance_AllowsFinalizedStatusTransitions(
        EventParticipationStatus currentStatus,
        EventParticipationStatus newStatus)
    {
        EventParticipation participation = CreateFinalizedParticipation(currentStatus);
        DateTime checkedInAtUtc = new(2026, 11, 9, 13, 10, 0, DateTimeKind.Utc);
        DateTime justifiedAtUtc = new(2026, 11, 9, 13, 20, 0, DateTimeKind.Utc);

        Correct(participation, newStatus, checkedInAtUtc, justifiedAtUtc);

        Assert.Equal(newStatus, participation.Status);
        Assert.Equal(
            newStatus == EventParticipationStatus.Present ? checkedInAtUtc : null,
            participation.CheckedInAtUtc);
        Assert.Equal(
            newStatus == EventParticipationStatus.JustifiedAbsence ? "Nova justificativa" : null,
            participation.Justification);
        Assert.Equal(
            newStatus == EventParticipationStatus.JustifiedAbsence ? justifiedAtUtc : null,
            participation.JustifiedAtUtc);
    }

    [Fact]
    public void CorrectAttendance_RejectsPendingParticipation()
    {
        EventParticipation participation = CreateParticipation();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            participation.CorrectAttendance(
                EventParticipationStatus.Present,
                "Correção administrativa",
                new DateTime(2026, 11, 9, 13, 10, 0, DateTimeKind.Utc)));

        Assert.Equal("Only finalized participation can be corrected.", exception.Message);
        Assert.Equal(EventParticipationStatus.Pending, participation.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrectAttendance_RejectsMissingAdministrativeReason(string? administrativeReason)
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.UnjustifiedAbsence,
            administrativeReason!));
    }

    [Fact]
    public void CorrectAttendance_RejectsPendingAsTargetStatus()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.Pending,
            "Correção administrativa"));
    }

    [Fact]
    public void CorrectAttendance_ToPresent_RequiresUtcCheckInTime()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.UnjustifiedAbsence);

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.Present,
            "Correção administrativa"));

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.Present,
            "Correção administrativa",
            new DateTime(2026, 11, 9, 13, 10, 0, DateTimeKind.Local)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrectAttendance_ToJustifiedAbsence_RequiresAbsenceJustification(
        string? absenceJustification)
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.JustifiedAbsence,
            "Correção administrativa",
            absenceJustification: absenceJustification,
            justifiedAtUtc: new DateTime(2026, 11, 9, 13, 20, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void CorrectAttendance_ToJustifiedAbsence_RequiresUtcJustificationTime()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.JustifiedAbsence,
            "Correção administrativa",
            absenceJustification: "Consulta médica"));

        Assert.Throws<ArgumentException>(() => participation.CorrectAttendance(
            EventParticipationStatus.JustifiedAbsence,
            "Correção administrativa",
            absenceJustification: "Consulta médica",
            justifiedAtUtc: new DateTime(2026, 11, 9, 13, 20, 0, DateTimeKind.Local)));
    }

    [Fact]
    public void CorrectAttendance_ToPresent_ClearsAbsenceData()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.JustifiedAbsence);
        DateTime checkedInAtUtc = new(2026, 11, 9, 13, 10, 0, DateTimeKind.Utc);

        participation.CorrectAttendance(
            EventParticipationStatus.Present,
            "Registro de presença confirmado",
            checkedInAtUtc);

        Assert.Equal(EventParticipationStatus.Present, participation.Status);
        Assert.Equal(checkedInAtUtc, participation.CheckedInAtUtc);
        Assert.Null(participation.Justification);
        Assert.Null(participation.JustifiedAtUtc);
    }

    [Fact]
    public void CorrectAttendance_ToJustifiedAbsence_ClearsCheckInData()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);
        DateTime justifiedAtUtc = new(2026, 11, 9, 13, 20, 0, DateTimeKind.Utc);

        participation.CorrectAttendance(
            EventParticipationStatus.JustifiedAbsence,
            "Atestado apresentado posteriormente",
            absenceJustification: "Consulta médica",
            justifiedAtUtc: justifiedAtUtc);

        Assert.Equal(EventParticipationStatus.JustifiedAbsence, participation.Status);
        Assert.Null(participation.CheckedInAtUtc);
        Assert.Equal("Consulta médica", participation.Justification);
        Assert.Equal(justifiedAtUtc, participation.JustifiedAtUtc);
    }

    [Fact]
    public void CorrectAttendance_ToUnjustifiedAbsence_ClearsPresenceAndJustificationData()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.JustifiedAbsence);

        participation.CorrectAttendance(
            EventParticipationStatus.UnjustifiedAbsence,
            "Justificativa administrativa não aprovada");

        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, participation.Status);
        Assert.Null(participation.CheckedInAtUtc);
        Assert.Null(participation.Justification);
        Assert.Null(participation.JustifiedAtUtc);
    }

    [Fact]
    public void CorrectAttendance_RejectsSameStatusWithoutEffectiveDataChange()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.Present);
        DateTime checkedInAtUtc = participation.CheckedInAtUtc!.Value;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            participation.CorrectAttendance(
                EventParticipationStatus.Present,
                "Motivo administrativo diferente",
                checkedInAtUtc));

        Assert.Equal("Attendance correction does not change participation.", exception.Message);
    }

    [Fact]
    public void CorrectAttendance_DoesNotStoreAdministrativeReasonInAbsenceJustification()
    {
        EventParticipation participation = CreateFinalizedParticipation(EventParticipationStatus.UnjustifiedAbsence);

        participation.CorrectAttendance(
            EventParticipationStatus.JustifiedAbsence,
            "Motivo administrativo",
            absenceJustification: "Consulta médica",
            justifiedAtUtc: new DateTime(2026, 11, 9, 13, 20, 0, DateTimeKind.Utc));

        Assert.Equal("Consulta médica", participation.Justification);
        Assert.NotEqual("Motivo administrativo", participation.Justification);
    }

    private static void Correct(
        EventParticipation participation,
        EventParticipationStatus newStatus,
        DateTime checkedInAtUtc,
        DateTime justifiedAtUtc)
    {
        if (newStatus == EventParticipationStatus.Present)
        {
            participation.CorrectAttendance(
                newStatus,
                "Correção administrativa",
                checkedInAtUtc);
        }
        else if (newStatus == EventParticipationStatus.JustifiedAbsence)
        {
            participation.CorrectAttendance(
                newStatus,
                "Correção administrativa",
                absenceJustification: "Nova justificativa",
                justifiedAtUtc: justifiedAtUtc);
        }
        else
        {
            participation.CorrectAttendance(newStatus, "Correção administrativa");
        }
    }

    private static EventParticipation CreateFinalizedParticipation(
        EventParticipationStatus status)
    {
        EventParticipation participation = CreateParticipation();
        DateTime utcNow = new(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);

        switch (status)
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
            default:
                throw new ArgumentException("Test requires a finalized status.", nameof(status));
        }

        return participation;
    }

    private static EventParticipation CreateParticipation()
    {
        return new EventParticipation(EventId, MemberId);
    }
}
