using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Xunit;

namespace IncludePresenca.Application.Tests.UseCases.Attendance;

public sealed class UpdateEventAttendanceUseCaseTests
{
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime StartsAt = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndsAt = StartsAt.AddHours(2);

    [Theory]
    [InlineData(EventParticipationStatus.Present)]
    [InlineData(EventParticipationStatus.JustifiedAbsence)]
    [InlineData(EventParticipationStatus.UnjustifiedAbsence)]
    public async Task ExecuteAsync_FinalizedParticipation_IsRejectedWithoutChangingIt(
        EventParticipationStatus finalizedStatus)
    {
        Event eventEntity = new("Event", null, StartsAt, EndsAt);
        EventParticipation participation = CreateParticipation(eventEntity, finalizedStatus);
        FakeUnitOfWork unitOfWork = new();
        UpdateEventAttendanceUseCase useCase = CreateUseCase(eventEntity, participation, unitOfWork);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                eventEntity.Id,
                MemberId,
                new UpdateAttendanceRequest(
                    nameof(EventParticipationStatus.UnjustifiedAbsence),
                    null)));

        Assert.Equal(
            "Only pending participation can be updated by the normal attendance flow.",
            exception.Message);
        Assert.Equal(finalizedStatus, participation.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PendingParticipation_StillUsesNormalFlow()
    {
        Event eventEntity = new("Event", null, StartsAt, EndsAt);
        EventParticipation participation = new(eventEntity.Id, MemberId);
        SetMember(participation);
        FakeUnitOfWork unitOfWork = new();
        UpdateEventAttendanceUseCase useCase = CreateUseCase(eventEntity, participation, unitOfWork);

        EventAttendanceResponse response = await useCase.ExecuteAsync(
            eventEntity.Id,
            MemberId,
            new UpdateAttendanceRequest(
                nameof(EventParticipationStatus.UnjustifiedAbsence),
                null));

        Assert.Equal(EventParticipationStatus.UnjustifiedAbsence, participation.Status);
        Assert.Equal(nameof(EventParticipationStatus.UnjustifiedAbsence), response.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Theory]
    [InlineData("PRESENTE", EventParticipationStatus.Present)]
    [InlineData("FALTA_JUSTIFICADA", EventParticipationStatus.JustifiedAbsence)]
    [InlineData("FALTA_NAO_JUSTIFICADA", EventParticipationStatus.UnjustifiedAbsence)]
    public async Task ExecuteAsync_FrontendStatusNames_AreAccepted(
        string status,
        EventParticipationStatus expectedStatus)
    {
        Event eventEntity = new("Event", null, StartsAt, EndsAt);
        EventParticipation participation = new(eventEntity.Id, MemberId);
        SetMember(participation);
        FakeUnitOfWork unitOfWork = new();
        UpdateEventAttendanceUseCase useCase = CreateUseCase(eventEntity, participation, unitOfWork);

        await useCase.ExecuteAsync(
            eventEntity.Id,
            MemberId,
            new UpdateAttendanceRequest(
                status,
                expectedStatus == EventParticipationStatus.JustifiedAbsence
                    ? "Consulta médica"
                    : null));

        Assert.Equal(expectedStatus, participation.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static UpdateEventAttendanceUseCase CreateUseCase(
        Event eventEntity,
        EventParticipation participation,
        FakeUnitOfWork unitOfWork) => new(
        new FakeEventRepository(eventEntity),
        new FakeParticipationRepository(participation),
        unitOfWork);

    private static EventParticipation CreateParticipation(
        Event eventEntity,
        EventParticipationStatus status)
    {
        EventParticipation participation = new(eventEntity.Id, MemberId);
        DateTime utcNow = StartsAt.AddMinutes(30);

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
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return participation;
    }

    private static void SetMember(EventParticipation participation)
    {
        typeof(EventParticipation)
            .GetProperty(nameof(EventParticipation.Member))!
            .SetValue(participation, new Member("Member", "member@example.com", Role.Membro));
    }

    private sealed class FakeEventRepository(Event eventEntity) : IEventRepository
    {
        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(id == eventEntity.Id ? eventEntity : null);

        public Task<List<Event>> GetAllAsync() =>
            Task.FromResult(new List<Event> { eventEntity });

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
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default) => operation();
    }
}
