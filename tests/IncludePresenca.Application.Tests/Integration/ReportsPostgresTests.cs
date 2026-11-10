using System.Globalization;
using System.Text;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class ReportsPostgresTests
{
    [PostgresFact]
    public async Task MemberReportAndCsv_UseTheSameRowsAndCalculations()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext context = await CreateDatabaseAsync(connectionString);
        DateTime start = DateTime.UtcNow.AddHours(-3);
        Member member = new("Report test", $"report-{Guid.NewGuid():N}@example.test", Role.Membro);
        member.ActivateAccount("test-password-hash");
        Event presentEvent = new("Present report event", null, start, start.AddHours(1));
        Event justifiedEvent = new("Justified report event", null, start.AddHours(-2), start.AddHours(-1));
        Event unjustifiedEvent = new("Unjustified report event", null, start.AddHours(-4), start.AddHours(-3));

        EventParticipation present = new(presentEvent.Id, member.Id);
        present.RegisterPresence(DateTime.SpecifyKind(presentEvent.StartsAt.AddMinutes(5), DateTimeKind.Utc));
        EventParticipation justified = new(justifiedEvent.Id, member.Id);
        justified.JustifyAbsence("Consulta", DateTime.SpecifyKind(justifiedEvent.EndsAt.AddHours(1), DateTimeKind.Utc));
        EventParticipation unjustified = new(unjustifiedEvent.Id, member.Id);
        unjustified.ConsolidateUnjustifiedAbsence();

        context.Members.Add(member);
        context.Events.AddRange(presentEvent, justifiedEvent, unjustifiedEvent);
        context.EventParticipations.AddRange(present, justified, unjustified);
        await context.SaveChangesAsync();

        FrequencyQueries frequencyQueries = new(context, new FrequencyCalculator());
        FrequencyReportQueries reportQueries = new(
            context,
            frequencyQueries,
            new FrequencyCalculator(),
            new FrequencyAggregator());

        var jsonReport = await reportQueries.GetMemberReportAsync();
        var summary = Assert.Single(jsonReport.Members, item => item.MemberId == member.Id);
        decimal frequencyPercentage = summary.FrequencyPercentage
            ?? throw new InvalidOperationException("Expected a frequency percentage for the test member.");
        string csv = Encoding.UTF8.GetString(new FrequencyReportCsvExporter().Export(jsonReport));
        string[] values = csv.Split(Environment.NewLine)
            .Single(line => line.Contains(member.Id.ToString(), StringComparison.Ordinal))
            .Split(',');

        Assert.Equal(1, summary.Counts.Presences);
        Assert.Equal(1, summary.Counts.JustifiedAbsences);
        Assert.Equal(1, summary.Counts.UnjustifiedAbsences);
        Assert.Equal(50m, summary.FrequencyPercentage);
        Assert.Equal(member.Id.ToString(), values[0].Trim('"'));
        Assert.Equal(summary.Counts.Presences.ToString(CultureInfo.InvariantCulture), values[2].Trim('"'));
        Assert.Equal(summary.Counts.JustifiedAbsences.ToString(CultureInfo.InvariantCulture), values[3].Trim('"'));
        Assert.Equal(summary.Counts.UnjustifiedAbsences.ToString(CultureInfo.InvariantCulture), values[4].Trim('"'));
        Assert.Equal(frequencyPercentage.ToString(CultureInfo.InvariantCulture), values[5].Trim('"'));

        var globalReport = await reportQueries.GetGeneralReportAsync();
        var directorSummary = Assert.Single(globalReport.Membros, item => item.Id == member.Id);

        Assert.Contains(globalReport.Membros, item => item.Id == member.Id);
        Assert.Equal(1, directorSummary.Presencas);
        Assert.Equal(3, directorSummary.TotalEventos);
        Assert.Equal(0, directorSummary.Atrasos);
        Assert.Equal(2, directorSummary.Faltas);
        Assert.Equal(50m, directorSummary.Frequencia);
    }

    [PostgresFact]
    public async Task EmptyMemberReport_ReturnsMembersWithNullPercentage()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext context = await CreateDatabaseAsync(connectionString);
        Member member = new("Empty report test", $"empty-report-{Guid.NewGuid():N}@example.test", Role.Membro);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        FrequencyQueries frequencyQueries = new(context, new FrequencyCalculator());
        FrequencyReportQueries reportQueries = new(
            context,
            frequencyQueries,
            new FrequencyCalculator(),
            new FrequencyAggregator());

        var report = await reportQueries.GetMemberReportAsync();
        var summary = Assert.Single(report.Members, item => item.MemberId == member.Id);

        Assert.Equal(0, summary.Counts.Presences);
        Assert.Equal(0, summary.Counts.JustifiedAbsences);
        Assert.Equal(0, summary.Counts.UnjustifiedAbsences);
        Assert.Null(summary.FrequencyPercentage);
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
}
