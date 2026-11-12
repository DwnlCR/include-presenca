using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class CorrectEventAttendancePostgresTests
{
    [PostgresFact]
    public async Task SuccessfulCorrection_PersistsParticipationAndCompleteAudit()
    {
        string connectionString = GetConnectionStringOrSkip();
        Scenario scenario = await CreateScenarioAsync(
            connectionString,
            EventParticipationStatus.JustifiedAbsence);
        DateTime checkedInAtUtc = scenario.StartsAt.AddMinutes(20);
        DateTime changedAtUtc = scenario.StartsAt.AddMinutes(30);

        await ExecuteAsync(
            connectionString,
            scenario,
            EventParticipationStatus.Present,
            "Lista assinada conferida pela Diretoria",
            checkedInAtUtc: checkedInAtUtc,
            timeProvider: new FixedTimeProvider(changedAtUtc));

        await using AppDbContext assertionContext = CreateContext(connectionString);
        EventParticipation participation = await assertionContext.EventParticipations
            .SingleAsync(item => item.Id == scenario.ParticipationId);
        EventParticipationAudit audit = await assertionContext.EventParticipationAudits
            .SingleAsync(item => item.ParticipationId == scenario.ParticipationId);

        Assert.Equal(EventParticipationStatus.Present, participation.Status);
        Assert.Equal(EventParticipationStatus.JustifiedAbsence, audit.PreviousStatus);
        Assert.Equal(EventParticipationStatus.Present, audit.NewStatus);
        AssertTimestampEqual(scenario.PreviousJustifiedAtUtc, audit.PreviousJustifiedAtUtc);
        Assert.Equal(scenario.PreviousJustification, audit.PreviousJustification);
        Assert.Null(audit.NewJustifiedAtUtc);
        Assert.Null(audit.NewJustification);
        Assert.Null(audit.PreviousCheckedInAtUtc);
        AssertTimestampEqual(checkedInAtUtc, audit.NewCheckedInAtUtc);
        Assert.Equal(scenario.DirectorId, audit.ChangedByMemberId);
        Assert.Equal("Lista assinada conferida pela Diretoria", audit.Reason);
        AssertTimestampEqual(changedAtUtc, audit.ChangedAtUtc);
        Assert.Equal(DateTimeKind.Utc, audit.ChangedAtUtc.Kind);
    }

    [PostgresFact]
    public async Task AuditPersistenceFailure_RollsBackParticipationChange()
    {
        string connectionString = GetConnectionStringOrSkip();
        Scenario scenario = await CreateScenarioAsync(
            connectionString,
            EventParticipationStatus.Present);
        Guid missingDirectorId = Guid.NewGuid();

        await Assert.ThrowsAsync<DbUpdateException>(() => ExecuteAsync(
            connectionString,
            scenario,
            EventParticipationStatus.UnjustifiedAbsence,
            "Falha de chave estrangeira na auditoria",
            responsibleDirectorId: missingDirectorId));

        await using AppDbContext assertionContext = CreateContext(connectionString);
        EventParticipation participation = await assertionContext.EventParticipations
            .SingleAsync(item => item.Id == scenario.ParticipationId);
        int auditCount = await assertionContext.EventParticipationAudits
            .CountAsync(item => item.ParticipationId == scenario.ParticipationId);

        Assert.Equal(EventParticipationStatus.Present, participation.Status);
        AssertTimestampEqual(scenario.PreviousCheckedInAtUtc, participation.CheckedInAtUtc);
        Assert.Equal(0, auditCount);
    }

    [PostgresFact]
    public async Task ConcurrentCorrections_SecondAuditUsesCommittedPreviousStatus()
    {
        string connectionString = GetConnectionStringOrSkip();
        Scenario scenario = await CreateScenarioAsync(
            connectionString,
            EventParticipationStatus.JustifiedAbsence);
        DateTime firstChangedAtUtc = scenario.StartsAt.AddMinutes(20);
        DateTime secondChangedAtUtc = scenario.StartsAt.AddMinutes(30);

        await using AppDbContext firstContext = CreateContext(connectionString);
        await using AppDbContext secondContext = CreateContext(connectionString);
        BlockingAuditRepository blockingAuditRepository = new(
            new EventParticipationAuditRepository(firstContext));
        SignalingParticipationRepository secondParticipationRepository = new(
            new EventParticipationRepository(secondContext));
        CorrectEventAttendanceUseCase firstUseCase = CreateUseCase(
            firstContext,
            blockingAuditRepository,
            new FixedTimeProvider(firstChangedAtUtc));
        CorrectEventAttendanceUseCase secondUseCase = CreateUseCase(
            secondContext,
            new EventParticipationAuditRepository(secondContext),
            new FixedTimeProvider(secondChangedAtUtc),
            secondParticipationRepository);

        Task firstCorrection = firstUseCase.ExecuteAsync(
            scenario.EventId,
            scenario.MemberId,
            scenario.DirectorId,
            EventParticipationStatus.Present,
            "Primeira correcao",
            checkedInAtUtc: scenario.StartsAt.AddMinutes(10));
        Task? secondCorrection = null;

        try
        {
            await blockingAuditRepository.AddStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            secondCorrection = secondUseCase.ExecuteAsync(
                scenario.EventId,
                scenario.MemberId,
                scenario.DirectorId,
                EventParticipationStatus.UnjustifiedAbsence,
                "Segunda correcao");

            await secondParticipationRepository.LookupStarted.Task
                .WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<TimeoutException>(() =>
                secondParticipationRepository.LookupCompleted.Task
                    .WaitAsync(TimeSpan.FromSeconds(2)));

            blockingAuditRepository.Release();
            await Task.WhenAll(firstCorrection, secondCorrection);
        }
        finally
        {
            blockingAuditRepository.Release();
            await ObserveFailureAsync(firstCorrection);
            if (secondCorrection is not null)
                await ObserveFailureAsync(secondCorrection);
        }

        await using AppDbContext assertionContext = CreateContext(connectionString);
        List<EventParticipationAudit> audits = await assertionContext.EventParticipationAudits
            .Where(item => item.ParticipationId == scenario.ParticipationId)
            .OrderBy(item => item.ChangedAtUtc)
            .ToListAsync();

        Assert.Equal(2, audits.Count);
        Assert.Equal(EventParticipationStatus.JustifiedAbsence, audits[0].PreviousStatus);
        Assert.Equal(EventParticipationStatus.Present, audits[0].NewStatus);
        Assert.Equal(EventParticipationStatus.Present, audits[1].PreviousStatus);
        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, audits[1].NewStatus);
    }

    [PostgresFact]
    public async Task SameEffectiveCorrection_DoesNotChangeDatabaseOrCreateAudit()
    {
        string connectionString = GetConnectionStringOrSkip();
        Scenario scenario = await CreateScenarioAsync(
            connectionString,
            EventParticipationStatus.Present);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(
            connectionString,
            scenario,
            EventParticipationStatus.Present,
            "Tentativa sem mudanca efetiva",
            checkedInAtUtc: scenario.PreviousCheckedInAtUtc));

        await using AppDbContext assertionContext = CreateContext(connectionString);
        EventParticipation participation = await assertionContext.EventParticipations
            .SingleAsync(item => item.Id == scenario.ParticipationId);
        int auditCount = await assertionContext.EventParticipationAudits
            .CountAsync(item => item.ParticipationId == scenario.ParticipationId);

        Assert.Equal(EventParticipationStatus.Present, participation.Status);
        AssertTimestampEqual(scenario.PreviousCheckedInAtUtc, participation.CheckedInAtUtc);
        Assert.Equal(0, auditCount);
    }

    private static async Task ObserveFailureAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
        }
    }

    private static async Task ExecuteAsync(
        string connectionString,
        Scenario scenario,
        EventParticipationStatus newStatus,
        string administrativeReason,
        DateTime? checkedInAtUtc = null,
        string? absenceJustification = null,
        DateTime? justifiedAtUtc = null,
        Guid? responsibleDirectorId = null,
        TimeProvider? timeProvider = null,
        IEventParticipationAuditRepository? auditRepository = null)
    {
        await using AppDbContext context = CreateContext(connectionString);
        CorrectEventAttendanceUseCase useCase = CreateUseCase(
            context,
            auditRepository ?? new EventParticipationAuditRepository(context),
            timeProvider ?? TimeProvider.System);

        await useCase.ExecuteAsync(
            scenario.EventId,
            scenario.MemberId,
            responsibleDirectorId ?? scenario.DirectorId,
            newStatus,
            administrativeReason,
            checkedInAtUtc,
            absenceJustification,
            justifiedAtUtc);
    }

    private static CorrectEventAttendanceUseCase CreateUseCase(
        AppDbContext context,
        IEventParticipationAuditRepository auditRepository,
        TimeProvider timeProvider,
        IEventParticipationRepository? participationRepository = null) =>
        new(
            new EventRepository(context),
            participationRepository ?? new EventParticipationRepository(context),
            auditRepository,
            new UnitOfWork(context),
            timeProvider);

    private static async Task<Scenario> CreateScenarioAsync(
        string connectionString,
        EventParticipationStatus status)
    {
        await using AppDbContext context = CreateContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        DateTime startsAt = DateTime.UtcNow.AddMinutes(-30);
        DateTime endsAt = startsAt.AddHours(2);
        Member member = new(
            $"Integration member {Guid.NewGuid():N}",
            $"member-{Guid.NewGuid():N}@example.test",
            Role.Membro);
        Member director = new(
            $"Integration director {Guid.NewGuid():N}",
            $"director-{Guid.NewGuid():N}@example.test",
            Role.Diretoria);
        Event eventEntity = new("Administrative correction integration", null, startsAt, endsAt);
        EventParticipation participation = new(eventEntity.Id, member.Id);

        DateTime? previousCheckedInAtUtc = null;
        string? previousJustification = null;
        DateTime? previousJustifiedAtUtc = null;

        switch (status)
        {
            case EventParticipationStatus.Present:
                previousCheckedInAtUtc = startsAt.AddMinutes(5);
                participation.RegisterPresence(previousCheckedInAtUtc.Value);
                break;
            case EventParticipationStatus.JustifiedAbsence:
                previousJustification = "Justificativa original";
                previousJustifiedAtUtc = startsAt.AddMinutes(5);
                participation.JustifyAbsence(
                    previousJustification,
                    previousJustifiedAtUtc.Value);
                break;
            case EventParticipationStatus.UnjustifiedAbsence:
                participation.ConsolidateUnjustifiedAbsence();
                break;
            default:
                throw new ArgumentException("Integration scenario requires a finalized status.", nameof(status));
        }

        context.Members.AddRange(member, director);
        context.Events.Add(eventEntity);
        context.EventParticipations.Add(participation);
        await context.SaveChangesAsync();

        return new Scenario(
            eventEntity.Id,
            member.Id,
            director.Id,
            participation.Id,
            startsAt,
            previousCheckedInAtUtc,
            previousJustification,
            previousJustifiedAtUtc);
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static string GetConnectionStringOrSkip()
    {
        string? connectionString = Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION");
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                "The PostgreSQL connection string should have been checked by PostgresFact.")
            : connectionString;
    }

    private static void AssertTimestampEqual(DateTime? expected, DateTime? actual)
    {
        Assert.Equal(TruncateToPostgresPrecision(expected), TruncateToPostgresPrecision(actual));
    }

    private static DateTime? TruncateToPostgresPrecision(DateTime? value)
    {
        if (value is null)
            return null;

        long precision = TimeSpan.TicksPerMicrosecond;
        long ticks = value.Value.Ticks;
        return new DateTime(ticks - ticks % precision, value.Value.Kind);
    }

    private sealed record Scenario(
        Guid EventId,
        Guid MemberId,
        Guid DirectorId,
        Guid ParticipationId,
        DateTime StartsAt,
        DateTime? PreviousCheckedInAtUtc,
        string? PreviousJustification,
        DateTime? PreviousJustifiedAtUtc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class BlockingAuditRepository : IEventParticipationAuditRepository
    {
        private readonly IEventParticipationAuditRepository _inner;
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingAuditRepository(IEventParticipationAuditRepository inner)
        {
            _inner = inner;
        }

        public TaskCompletionSource<bool> AddStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task AddAsync(
            EventParticipationAudit audit,
            CancellationToken cancellationToken = default)
        {
            await _inner.AddAsync(audit, cancellationToken);
            AddStarted.TrySetResult(true);
            await _release.Task;
        }

        public void Release() => _release.TrySetResult(true);
    }

    private sealed class SignalingParticipationRepository : IEventParticipationRepository
    {
        private readonly IEventParticipationRepository _inner;

        public SignalingParticipationRepository(IEventParticipationRepository inner)
        {
            _inner = inner;
        }

        public TaskCompletionSource<bool> LookupStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> LookupCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task AddRangeAsync(List<EventParticipation> participations) =>
            _inner.AddRangeAsync(participations);

        public Task<List<EventParticipation>> GetByEventIdAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            _inner.GetByEventIdAsync(eventId, cancellationToken);

        public Task<List<EventParticipation>> GetByEventIdsAsync(
            List<Guid> eventIds,
            CancellationToken cancellationToken = default) =>
            _inner.GetByEventIdsAsync(eventIds, cancellationToken);

        public async Task<EventParticipation?> GetByEventAndMemberIdAsync(
            Guid eventId,
            Guid memberId,
            bool forUpdate = false,
            CancellationToken cancellationToken = default)
        {
            LookupStarted.TrySetResult(true);

            try
            {
                return await _inner.GetByEventAndMemberIdAsync(
                    eventId,
                    memberId,
                    forUpdate,
                    cancellationToken);
            }
            finally
            {
                LookupCompleted.TrySetResult(true);
            }
        }
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")))
            {
                Skip = "Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.";
            }
        }
    }
}
