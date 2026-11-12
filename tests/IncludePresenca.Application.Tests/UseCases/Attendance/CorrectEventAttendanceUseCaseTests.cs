using IncludePresenca.Application.Interfaces;
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
    public async Task ExecuteAsync_ValidCorrection_PersistsParticipationAndAuditAtomically()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);
        DateTime checkedInAtUtc = EventStartsAt.AddMinutes(20);

        await fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.Present,
            "Administrative attendance correction",
            checkedInAtUtc: checkedInAtUtc);

        Assert.Equal(EventParticipationStatus.Present, fixture.Participation.Status);
        Assert.Equal(checkedInAtUtc, fixture.Participation.CheckedInAtUtc);
        Assert.Single(fixture.AuditRepository.Audits);
        EventParticipationAudit audit = fixture.AuditRepository.Audits[0];
        Assert.Equal(fixture.Participation.Id, audit.ParticipationId);
        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, audit.PreviousStatus);
        Assert.Equal(EventParticipationStatus.Present, audit.NewStatus);
        Assert.Null(audit.PreviousCheckedInAtUtc);
        Assert.Equal(checkedInAtUtc, audit.NewCheckedInAtUtc);
        Assert.Equal(DirectorId, audit.ChangedByMemberId);
        Assert.Equal(DateTimeKind.Utc, audit.ChangedAtUtc.Kind);
        Assert.Equal(1, fixture.ParticipationRepository.LookupCount);
        Assert.True(fixture.ParticipationRepository.LastForUpdate);
        Assert.Equal(1, fixture.UnitOfWork.TransactionCount);
    }

    [Fact]
    public async Task ExecuteAsync_JustifiedAbsence_PersistsPreviousAndNewValuesInAudit()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        DateTime previousCheckIn = fixture.Participation.CheckedInAtUtc!.Value;
        DateTime justifiedAtUtc = EventStartsAt.AddMinutes(30);

        await fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.JustifiedAbsence,
            "Administrative correction",
            absenceJustification: "Medical appointment",
            justifiedAtUtc: justifiedAtUtc);

        EventParticipationAudit audit = Assert.Single(fixture.AuditRepository.Audits);
        Assert.Equal(EventParticipationStatus.Present, audit.PreviousStatus);
        Assert.Equal(EventParticipationStatus.JustifiedAbsence, audit.NewStatus);
        Assert.Equal(previousCheckIn, audit.PreviousCheckedInAtUtc);
        Assert.Null(audit.NewCheckedInAtUtc);
        Assert.Equal("Medical appointment", audit.NewJustification);
        Assert.Equal(justifiedAtUtc, audit.NewJustifiedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_UnjustifiedAbsence_ClearsIncompatibleAttendanceData()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.JustifiedAbsence);

        await fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Administrative correction");

        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, fixture.Participation.Status);
        Assert.Null(fixture.Participation.CheckedInAtUtc);
        Assert.Null(fixture.Participation.Justification);
        Assert.Null(fixture.Participation.JustifiedAtUtc);
        EventParticipationAudit audit = Assert.Single(fixture.AuditRepository.Audits);
        Assert.Null(audit.NewCheckedInAtUtc);
        Assert.Null(audit.NewJustification);
        Assert.Null(audit.NewJustifiedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_EventDoesNotExist_RejectsCorrectionWithoutStartingParticipationLookup()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.UseCase.ExecuteAsync(
            Guid.NewGuid(), MemberId, DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Administrative correction"));

        Assert.Equal(0, fixture.ParticipationRepository.LookupCount);
        Assert.Empty(fixture.AuditRepository.Audits);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledEvent_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        fixture.Event.Cancel();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Administrative correction"));

        Assert.Empty(fixture.AuditRepository.Audits);
    }

    [Fact]
    public async Task ExecuteAsync_ParticipationDoesNotExist_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);
        fixture.ParticipationRepository.Participation = null;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.UnjustifiedAbsence,
            "Administrative correction"));

        Assert.Empty(fixture.AuditRepository.Audits);
    }

    [Fact]
    public async Task ExecuteAsync_PresentOutsideEventPeriod_RejectsCorrectionWithoutAudit()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.Present,
            "Administrative correction",
            checkedInAtUtc: EventEndsAt));

        Assert.Empty(fixture.AuditRepository.Audits);
    }

    [Fact]
    public async Task ExecuteAsync_PresentWithNonUtcTime_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.UnjustifiedAbsence);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.Present,
            "Administrative correction",
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
            fixture.Event.Id, MemberId, DirectorId,
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
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.JustifiedAbsence,
            "Administrative correction",
            absenceJustification: absenceJustification,
            justifiedAtUtc: EventStartsAt.AddMinutes(30)));
    }

    [Fact]
    public async Task ExecuteAsync_MissingJustificationTimestamp_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.JustifiedAbsence,
            "Administrative correction",
            absenceJustification: "Medical appointment"));
    }

    [Fact]
    public async Task ExecuteAsync_PendingTargetStatus_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.Pending,
            "Administrative correction"));
    }

    [Fact]
    public async Task ExecuteAsync_PendingParticipation_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Pending);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.Present,
            "Administrative correction",
            checkedInAtUtc: EventStartsAt.AddMinutes(20)));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDirectorId_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, Guid.Empty,
            EventParticipationStatus.UnjustifiedAbsence,
            "Administrative correction"));
    }

    [Fact]
    public async Task ExecuteAsync_SameStatusWithoutEffectiveChange_RejectsCorrection()
    {
        Fixture fixture = CreateFixture(EventParticipationStatus.Present);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.ExecuteAsync(
            fixture.Event.Id, MemberId, DirectorId,
            EventParticipationStatus.Present,
            "Administrative correction",
            checkedInAtUtc: fixture.Participation.CheckedInAtUtc));
    }

    private static Fixture CreateFixture(EventParticipationStatus status)
    {
        Event eventEntity = new("Event", null, EventStartsAt, EventEndsAt);
        EventParticipation participation = new(eventEntity.Id, MemberId);
        SetStatus(participation, status);

        FakeParticipationRepository participationRepository = new(participation);
        FakeAuditRepository auditRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CorrectEventAttendanceUseCase useCase = new(
            new FakeEventRepository(eventEntity),
            participationRepository,
            auditRepository,
            unitOfWork,
            new FixedTimeProvider(EventStartsAt.AddHours(1)));

        return new Fixture(
            eventEntity,
            participation,
            participationRepository,
            auditRepository,
            unitOfWork,
            useCase);
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
                participation.JustifyAbsence("Medical appointment", utcNow);
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
        FakeAuditRepository AuditRepository,
        FakeUnitOfWork UnitOfWork,
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
        public bool LastForUpdate { get; private set; }

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
            LastForUpdate = forUpdate;
            return Task.FromResult(Participation);
        }
    }

    private sealed class FakeAuditRepository : IEventParticipationAuditRepository
    {
        public List<EventParticipationAudit> Audits { get; } = [];

        public Task AddAsync(
            EventParticipationAudit audit,
            CancellationToken cancellationToken = default)
        {
            Audits.Add(audit);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int TransactionCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public async Task ExecuteInTransactionAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            await operation();
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
