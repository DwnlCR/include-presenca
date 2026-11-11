using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Xunit;

namespace IncludePresenca.Application.Tests.UseCases.Attendance;

public sealed class CorrectEventAttendanceUseCaseTests
{
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DirectorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTime EventStartsAt =
        new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EventEndsAt = EventStartsAt.AddHours(2);

    [Fact]
    public async Task ExecuteAsync_ValidCorrection_IsBlockedUntilAuditPersistenceExists()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.UseCase.ExecuteAsync(
                fixture.Event.Id,
                MemberId,
                DirectorId,
                EventParticipationStatus.Present,
                "Presença confirmada em lista assinada",
                checkedInAtUtc: EventStartsAt.AddMinutes(20)));

        Assert.Equal(
            "Administrative attendance correction cannot be persisted before audit support is available.",
            exception.Message);
        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, fixture.Participation.Status);
        Assert.Null(fixture.Participation.CheckedInAtUtc);
        Assert.Equal(1, fixture.ParticipationRepository.LookupCount);
    }

    [Fact]
    public async Task ExecuteAsync_EventDoesNotExist_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        Guid missingEventId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.UseCase.ExecuteAsync(
            missingEventId,
            MemberId,
            DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Correção administrativa"));

        Assert.Equal(0, fixture.ParticipationRepository.LookupCount);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledEvent_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        fixture.Event.Cancel();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Correção administrativa"));

        Assert.Equal(0, fixture.ParticipationRepository.LookupCount);
    }

    [Fact]
    public async Task ExecuteAsync_ParticipationDoesNotExist_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        fixture.ParticipationRepository.Participation = null;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Correção administrativa"));
    }

    [Fact]
    public async Task ExecuteAsync_PresentOutsideEventPeriod_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Present,
            "Presença confirmada",
            checkedInAtUtc: EventEndsAt));
    }

    [Fact]
    public async Task ExecuteAsync_PresentWithNonUtcTime_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Present,
            "Presença confirmada",
            checkedInAtUtc: new DateTime(2026, 11, 11, 13, 0, 0, DateTimeKind.Local)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_MissingAdministrativeReason_RejectsCorrection(
        string? administrativeReason)
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            administrativeReason!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_MissingAbsenceJustification_RejectsCorrection(
        string? absenceJustification)
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.JustifiedAbsence,
            "Atestado apresentado",
            absenceJustification: absenceJustification,
            justifiedAtUtc: EventStartsAt.AddMinutes(30)));
    }

    [Fact]
    public async Task ExecuteAsync_MissingJustificationTimestamp_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.JustifiedAbsence,
            "Atestado apresentado",
            absenceJustification: "Consulta médica"));
    }

    [Fact]
    public async Task ExecuteAsync_PendingTargetStatus_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Pending,
            "Correção administrativa"));
    }

    [Fact]
    public async Task ExecuteAsync_PendingParticipation_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Pending);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Present,
            "Presença confirmada",
            checkedInAtUtc: EventStartsAt.AddMinutes(20)));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDirectorId_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            Guid.Empty,
            EventParticipationStatus.UnjustifiedAbsence,
            "Correção administrativa"));
    }

    [Fact]
    public async Task ExecuteAsync_SameStatusWithoutEffectiveChange_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        DateTime checkedInAtUtc = fixture.Participation.CheckedInAtUtc!.Value;

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Present,
            "Motivo administrativo",
            checkedInAtUtc: checkedInAtUtc));
    }

    private static Fixture CreateFixture(EventParticipationStatus status)
    {
        Event eventEntity = new("Event", null, EventStartsAt, EventEndsAt);
        EventParticipation participation = new(eventEntity.Id, MemberId);
        SetStatus(participation, status);

        FakeParticipationRepository participationRepository = new(participation);
        CorrectEventAttendanceUseCase useCase = new(
            new FakeEventRepository(eventEntity),
            participationRepository);

        return new Fixture(eventEntity, participation, participationRepository, useCase);
    }

    private static void SetStatus(
        EventParticipation participation,
        EventParticipationStatus status)
    {
        DateTime utcNow = EventStartsAt.AddMinutes(10);

        switch (status)
        {
            case EventParticipationStatus.Pending:
                return;
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
    }

    private sealed record Fixture(
        Event Event,
        EventParticipation Participation,
        FakeParticipationRepository ParticipationRepository,
        CorrectEventAttendanceUseCase UseCase);

    private sealed class FakeEventRepository(Event eventEntity) : IEventRepository
    {
        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(id == eventEntity.Id ? eventEntity : null);

        public Task<List<Event>> GetAllAsync() =>
            Task.FromResult(new List<Event> { eventEntity });

        public Task AddAsync(Event eventEntity) => Task.CompletedTask;
    }

    private sealed class FakeParticipationRepository(EventParticipation? participation)
        : IEventParticipationRepository
    {
        public EventParticipation? Participation { get; set; } = participation;
        public int LookupCount { get; private set; }

        public Task AddRangeAsync(List<EventParticipation> participations) =>
            Task.CompletedTask;

        public Task<List<EventParticipation>> GetByEventIdAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Participation is null
                ? new List<EventParticipation>()
                : new List<EventParticipation> { Participation });

        public Task<EventParticipation?> GetByEventAndMemberIdAsync(
            Guid eventId,
            Guid memberId,
            bool forUpdate = false,
            CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(Participation);
        }
    }
}
