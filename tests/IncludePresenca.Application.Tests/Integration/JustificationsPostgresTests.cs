using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces.Queries;
using IncludePresenca.Application.Interfaces.Storage;
using IncludePresenca.Application.Services;
using IncludePresenca.Application.UseCases.Justifications;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Persistence.Queries;
using IncludePresenca.Infrastructure.Persistence.Repositories;
using IncludePresenca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class JustificationsPostgresTests
{
    [PostgresFact]
    public async Task Submit_List_Approve_UpdatesFrequencyAndPersistsAuditAndAttachment()
    {
        string connectionString = GetConnectionStringOrSkip();
        string root = CreateStorageRoot();
        try
        {
            Scenario scenario = await CreateScenarioAsync(connectionString, root);
            await using AppDbContext context = CreateContext(connectionString);
            SubmitJustificationUseCase submit = CreateSubmit(context, root, new FixedTimeProvider(scenario.Now));
            CreatedJustificationResponse created = await submit.ExecuteAsync(
                scenario.MemberId,
                new SubmitJustificationRequest
                {
                    FaltaId = scenario.AbsenceParticipationId,
                    Motivo = JustificationReasonDto.SAUDE,
                    Descricao = new string('a', 50),
                    Anexo = new AttachmentUpload(
                        new MemoryStream("%PDF-1.7"u8.ToArray()),
                        "comprovante.pdf",
                        "application/pdf",
                        8)
                });

            IJustificationQueries queries = new JustificationQueries(context, new FrequencyCalculator());
            Assert.Empty(await queries.GetPendingAbsencesAsync(scenario.MemberId));
            Assert.Equal(JustificationStatusDto.EM_ANALISE, (await queries.GetMineAsync(scenario.MemberId)).Single().Status);

            await using AppDbContext decisionContext = CreateContext(connectionString);
            DecideJustificationUseCase decide = CreateDecide(decisionContext, new FixedTimeProvider(scenario.Now.AddMinutes(1)));
            await decide.ExecuteAsync(created.Id, scenario.DirectorId, JustificationStatusDto.APROVADA);

            await using AppDbContext assertionContext = CreateContext(connectionString);
            EventParticipation participation = await assertionContext.EventParticipations.SingleAsync(x => x.Id == scenario.AbsenceParticipationId);
            JustificationRequest request = await assertionContext.JustificationRequests.SingleAsync(x => x.Id == created.Id);
            JustificationAudit audit = await assertionContext.JustificationAudits.SingleAsync(x => x.JustificationRequestId == created.Id);
            Assert.Equal(EventParticipationStatus.JustifiedAbsence, participation.Status);
            Assert.Equal(JustificationRequestStatus.Approved, request.Status);
            Assert.Equal("JustificationDecision", audit.Origin);
            Assert.Equal(scenario.DirectorId, audit.ChangedByMemberId);
            Assert.True(File.Exists(Path.Combine(root, request.AttachmentStorageKey!.Replace('/', Path.DirectorySeparatorChar))));

            FrequencyQueries frequency = new(assertionContext, new FrequencyCalculator());
            var summary = await frequency.GetMemberFrequencyAsync(scenario.MemberId);
            Assert.Equal(100m, summary!.Percentual);
        }
        finally
        {
            DeleteStorageRoot(root);
        }
    }

    [PostgresFact]
    public async Task Reject_KeepsUnjustifiedAbsenceAndPersistsRejectedAudit()
    {
        string connectionString = GetConnectionStringOrSkip();
        string root = CreateStorageRoot();
        try
        {
            Scenario scenario = await CreateScenarioAsync(connectionString, root);
            await using AppDbContext context = CreateContext(connectionString);
            CreatedJustificationResponse created = await CreateSubmit(context, root, new FixedTimeProvider(scenario.Now))
                .ExecuteAsync(scenario.MemberId, RequestFor(scenario));

            await using AppDbContext decisionContext = CreateContext(connectionString);
            await CreateDecide(decisionContext, new FixedTimeProvider(scenario.Now.AddMinutes(1)))
                .ExecuteAsync(created.Id, scenario.DirectorId, JustificationStatusDto.REJEITADA);

            await using AppDbContext assertionContext = CreateContext(connectionString);
            Assert.Equal(EventParticipationStatus.UnjustifiedAbsence,
                (await assertionContext.EventParticipations.SingleAsync(x => x.Id == scenario.AbsenceParticipationId)).Status);
            Assert.Equal(JustificationRequestStatus.Rejected,
                (await assertionContext.JustificationRequests.SingleAsync(x => x.Id == created.Id)).Status);
            Assert.Equal(JustificationRequestStatus.Rejected,
                (await assertionContext.JustificationAudits.SingleAsync(x => x.JustificationRequestId == created.Id)).NewStatus);
        }
        finally
        {
            DeleteStorageRoot(root);
        }
    }

    [PostgresFact]
    public async Task DuplicateSubmission_IsRejected()
    {
        string connectionString = GetConnectionStringOrSkip();
        string root = CreateStorageRoot();
        try
        {
            Scenario scenario = await CreateScenarioAsync(connectionString, root);
            await using AppDbContext context = CreateContext(connectionString);
            SubmitJustificationUseCase useCase = CreateSubmit(context, root, new FixedTimeProvider(scenario.Now));
            await useCase.ExecuteAsync(scenario.MemberId, RequestFor(scenario));

            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(scenario.MemberId, RequestFor(scenario)));
        }
        finally
        {
            DeleteStorageRoot(root);
        }
    }

    [PostgresFact]
    public async Task ConcurrentSubmissions_LeaveOnlyOnePendingRequest()
    {
        string connectionString = GetConnectionStringOrSkip();
        string root = CreateStorageRoot();
        try
        {
            Scenario scenario = await CreateScenarioAsync(connectionString, root);
            Task<CreatedJustificationResponse> first = SubmitOnNewContext(connectionString, root, scenario);
            Task<CreatedJustificationResponse> second = SubmitOnNewContext(connectionString, root, scenario);
            Task all = Task.WhenAll(first, second);

            await Assert.ThrowsAnyAsync<Exception>(() => all);
            await using AppDbContext assertionContext = CreateContext(connectionString);
            Assert.Equal(1, await assertionContext.JustificationRequests.CountAsync(x => x.ParticipationId == scenario.AbsenceParticipationId));
        }
        finally
        {
            DeleteStorageRoot(root);
        }
    }

    private static async Task<CreatedJustificationResponse> SubmitOnNewContext(string connectionString, string root, Scenario scenario)
    {
        await using AppDbContext context = CreateContext(connectionString);
        return await CreateSubmit(context, root, new FixedTimeProvider(scenario.Now)).ExecuteAsync(scenario.MemberId, RequestFor(scenario));
    }

    private static SubmitJustificationRequest RequestFor(Scenario scenario) => new()
    {
        FaltaId = scenario.AbsenceParticipationId,
        Motivo = JustificationReasonDto.OUTRO,
        Descricao = new string('b', 50)
    };

    private static SubmitJustificationUseCase CreateSubmit(AppDbContext context, string root, TimeProvider timeProvider) => new(
        new JustificationParticipationRepository(context),
        new JustificationRequestRepository(context),
        new PrivateFileStorage(Options.Create(new PrivateFileStorageOptions { RootPath = root })),
        new UnitOfWork(context), timeProvider);

    private static DecideJustificationUseCase CreateDecide(AppDbContext context, TimeProvider timeProvider) => new(
        new JustificationRequestRepository(context),
        new JustificationParticipationRepository(context),
        new JustificationAuditRepository(context),
        new UnitOfWork(context), timeProvider);

    private static async Task<Scenario> CreateScenarioAsync(string connectionString, string root)
    {
        await using AppDbContext context = CreateContext(connectionString);
        await context.Database.MigrateAsync();
        DateTime now = DateTime.UtcNow;
        DateTime absenceStart = now.AddHours(-2);
        DateTime presentStart = now.AddHours(-5);
        Member member = new($"Member {Guid.NewGuid():N}", $"member-{Guid.NewGuid():N}@test.local", Role.Membro);
        Member director = new($"Director {Guid.NewGuid():N}", $"director-{Guid.NewGuid():N}@test.local", Role.Diretoria);
        Event absenceEvent = new("Absence event", null, absenceStart, absenceStart.AddMinutes(30));
        Event presentEvent = new("Present event", null, presentStart, presentStart.AddMinutes(30));
        EventParticipation absence = new(absenceEvent.Id, member.Id);
        absence.ConsolidateUnjustifiedAbsence();
        EventParticipation present = new(presentEvent.Id, member.Id);
        present.RegisterPresence(presentStart.AddMinutes(5));
        context.Members.AddRange(member, director);
        context.Events.AddRange(absenceEvent, presentEvent);
        context.EventParticipations.AddRange(absence, present);
        await context.SaveChangesAsync();
        return new Scenario(member.Id, director.Id, absence.Id, now);
    }

    private static AppDbContext CreateContext(string connectionString) => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
    private static string GetConnectionStringOrSkip() => Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")
        ?? throw new InvalidOperationException("Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.");
    private static string CreateStorageRoot() => Path.Combine(Path.GetTempPath(), "include-presenca-justifications", Guid.NewGuid().ToString("N"));
    private static void DeleteStorageRoot(string root) { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private sealed record Scenario(Guid MemberId, Guid DirectorId, Guid AbsenceParticipationId, DateTime Now);
    private sealed class FixedTimeProvider(DateTime now) : TimeProvider { public override DateTimeOffset GetUtcNow() => new(now); }
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")))
                Skip = "Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.";
        }
    }
}
