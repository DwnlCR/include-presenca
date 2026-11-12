using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Services;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using Xunit;

namespace IncludePresenca.Application.Tests.UseCases.Attendance;

public sealed class CheckInEventUseCaseTests
{
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime StartsAt = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndsAt = StartsAt.AddHours(2);

    [Fact]
    public async Task ExecuteAsync_BeforeEventStart_RejectsCheckIn()
    {
        CheckInFixture fixture = CreateFixture(StartsAt.AddTicks(-1));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.UseCase.ExecuteAsync(fixture.Event.Id, MemberId, fixture.Code));

        Assert.Equal("Check-in is only available during the event.", exception.Message);
        Assert.False(fixture.Participation.CheckedInAtUtc.HasValue);
    }

    [Fact]
    public async Task ExecuteAsync_ExactlyAtEventStart_AllowsCheckIn()
    {
        CheckInFixture fixture = CreateFixture(StartsAt);

        CheckInResponse response = await fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            fixture.Code);

        Assert.Equal(fixture.Participation.Id, response.ParticipacaoId);
        Assert.Equal(StartsAt, fixture.Participation.CheckedInAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_DuringEvent_AllowsCheckIn()
    {
        DateTime checkedInAt = StartsAt.AddHours(1);
        CheckInFixture fixture = CreateFixture(checkedInAt);

        CheckInResponse response = await fixture.UseCase.ExecuteAsync(
            fixture.Event.Id,
            MemberId,
            fixture.Code);

        Assert.Equal(fixture.Participation.Id, response.ParticipacaoId);
        Assert.Equal(checkedInAt, fixture.Participation.CheckedInAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ExactlyAtEventEnd_RejectsCheckIn()
    {
        CheckInFixture fixture = CreateFixture(EndsAt);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.UseCase.ExecuteAsync(fixture.Event.Id, MemberId, fixture.Code));

        Assert.False(fixture.Participation.CheckedInAtUtc.HasValue);
    }

    [Fact]
    public async Task ExecuteAsync_AfterEventEnd_RejectsCheckIn()
    {
        CheckInFixture fixture = CreateFixture(EndsAt.AddTicks(1));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.UseCase.ExecuteAsync(fixture.Event.Id, MemberId, fixture.Code));

        Assert.False(fixture.Participation.CheckedInAtUtc.HasValue);
    }

    private static CheckInFixture CreateFixture(DateTime nowUtc)
    {
        Event eventEntity = new("Event", null, StartsAt, EndsAt);
        EventParticipation participation = new(eventEntity.Id, MemberId);
        CheckInCodeGenerator codeGenerator = new();
        string code = "123456";
        EventCheckInCode checkInCode = new(
            eventEntity.Id,
            codeGenerator.Hash(code),
            StartsAt.AddHours(-1),
            EndsAt.AddHours(1));

        CheckInEventUseCase useCase = new(
            new FakeEventRepository(eventEntity),
            new FakeParticipationRepository(participation),
            new FakeCheckInCodeRepository(checkInCode),
            new FakeUnitOfWork(),
            new CheckInCodeValidator(),
            codeGenerator,
            new FixedTimeProvider(nowUtc));

        return new CheckInFixture(eventEntity, participation, useCase, code);
    }

    private sealed record CheckInFixture(
        Event Event,
        EventParticipation Participation,
        CheckInEventUseCase UseCase,
        string Code);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class FakeEventRepository(Event eventEntity) : IEventRepository
    {
        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(id == eventEntity.Id ? eventEntity : null);

        public Task<List<Event>> GetAllAsync() => Task.FromResult(new List<Event> { eventEntity });

        public Task AddAsync(Event eventEntity) => Task.CompletedTask;
    }

    private sealed class FakeParticipationRepository(EventParticipation participation)
        : IEventParticipationRepository
    {
        public Task AddRangeAsync(List<EventParticipation> participations) => Task.CompletedTask;

        public Task<List<EventParticipation>> GetByEventIdAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EventParticipation> { participation });

        public Task<EventParticipation?> GetByEventAndMemberIdAsync(
            Guid eventId,
            Guid memberId,
            bool forUpdate = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EventParticipation?>(
                eventId == participation.EventId && memberId == participation.MemberId
                    ? participation
                    : null);

        public Task<List<EventParticipation>> GetByEventIdsAsync(
            List<Guid> eventIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<EventParticipation>());
        }
    }

    private sealed class FakeCheckInCodeRepository(EventCheckInCode checkInCode)
        : IEventCheckInCodeRepository
    {
        public Task LockEventForCodeGenerationAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<EventCheckInCode?> GetByEventIdAsync(
            Guid eventId,
            bool forUpdate = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EventCheckInCode?>(
                eventId == checkInCode.EventId ? checkInCode : null);

        public Task AddAsync(EventCheckInCode code, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public async Task ExecuteInTransactionAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default) => await operation();
    }
}
