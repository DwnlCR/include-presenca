using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class FrequencyQueriesPostgresTests
{
    [PostgresFact]
    public async Task History_WithDaysFilter_UsesEventStartAndPreservesStatusData()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext context = await CreateDatabaseAsync(connectionString);
        DateTime now = DateTime.UtcNow;
        Member member = new("Frequency test", $"frequency-{Guid.NewGuid():N}@example.test", Role.Membro);
        Event recent = CreateEvent("Recent event", now.AddDays(-10));
        Event older = CreateEvent("Older event", now.AddDays(-40));
        Event outside = CreateEvent("Outside event", now.AddDays(-100));

        EventParticipation recentParticipation = new(recent.Id, member.Id);
        recentParticipation.RegisterPresence(DateTime.SpecifyKind(recent.StartsAt.AddMinutes(11), DateTimeKind.Utc));
        EventParticipation olderParticipation = new(older.Id, member.Id);
        olderParticipation.JustifyAbsence("Consulta", DateTime.SpecifyKind(older.EndsAt.AddHours(1), DateTimeKind.Utc));
        EventParticipation outsideParticipation = new(outside.Id, member.Id);
        outsideParticipation.ConsolidateUnjustifiedAbsence();

        context.Members.Add(member);
        context.Events.AddRange(recent, older, outside);
        context.EventParticipations.AddRange(recentParticipation, olderParticipation, outsideParticipation);
        await context.SaveChangesAsync();

        FrequencyQueries queries = new(context, new FrequencyCalculator());

        IReadOnlyList<ParticipationHistoryItemResponse> filtered =
            await queries.GetMemberHistoryAsync(member.Id, 60);
        IReadOnlyList<ParticipationHistoryItemResponse> all =
            await queries.GetMemberHistoryAsync(member.Id);

        Assert.Equal(2, filtered.Count);
        Assert.Equal([recent.Id, older.Id], filtered.Select(item => item.EventoId));
        Assert.Equal(ParticipationStatus.Late, filtered[0].Status);
        Assert.Equal("Consulta", filtered[1].Observacao);
        Assert.Equal(3, all.Count);
    }

    [PostgresTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task History_WithNonPositiveDays_ThrowsBadRequestCompatibleException(int days)
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext context = await CreateDatabaseAsync(connectionString);
        FrequencyQueries queries = new(context, new FrequencyCalculator());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => queries.GetMemberHistoryAsync(Guid.NewGuid(), days));
    }

    private static Event CreateEvent(string title, DateTime startsAt)
    {
        DateTime utcStart = DateTime.SpecifyKind(startsAt, DateTimeKind.Utc);
        return new Event(title, null, utcStart, utcStart.AddHours(1));
    }

    private static async Task<AppDbContext> CreateDatabaseAsync(string connectionString)
    {
        AppDbContext context = CreateContext(connectionString);
        await context.Database.EnsureCreatedAsync();
        return context;
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
            ? throw new InvalidOperationException("The PostgreSQL connection string should have been checked by PostgresFact.")
            : connectionString;
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")))
                Skip = "Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.";
        }
    }

    private sealed class PostgresTheoryAttribute : TheoryAttribute
    {
        public PostgresTheoryAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")))
                Skip = "Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.";
        }
    }
}
