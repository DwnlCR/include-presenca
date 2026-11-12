using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class JustificationsHttpTests : IClassFixture<JustificationsApiFactory>
{
    private readonly JustificationsApiFactory _factory;

    public JustificationsHttpTests(JustificationsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoints_EnforceAuthenticationAuthorizationAndMemberIsolation()
    {
        await using JustificationsApiFactory.Scenario scenario = await _factory.CreateScenarioAsync();
        using HttpClient anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/me/faltas")).StatusCode);

        using HttpClient member = _factory.CreateClientWithToken(scenario.MemberToken);
        using HttpClient otherMember = _factory.CreateClientWithToken(scenario.OtherMemberToken);
        using HttpClient director = _factory.CreateClientWithToken(scenario.DirectorToken);

        HttpResponseMessage memberAbsences = await member.GetAsync("/api/me/faltas");
        Assert.Equal(HttpStatusCode.OK, memberAbsences.StatusCode);
        using JsonDocument absencesJson = await JsonDocument.ParseAsync(await memberAbsences.Content.ReadAsStreamAsync());
        Assert.Equal(1, absencesJson.RootElement.GetArrayLength());
        Assert.True(Guid.TryParse(absencesJson.RootElement[0].GetProperty("id").GetString(), out _));

        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.GetAsync("/api/justificativas")).StatusCode);

        HttpResponseMessage otherMemberAbsences = await otherMember.GetAsync("/api/me/faltas");
        Assert.Equal(HttpStatusCode.OK, otherMemberAbsences.StatusCode);
        using JsonDocument otherAbsencesJson = await JsonDocument.ParseAsync(await otherMemberAbsences.Content.ReadAsStreamAsync());
        Assert.Equal(1, otherAbsencesJson.RootElement.GetArrayLength());
        Assert.NotEqual(
            absencesJson.RootElement[0].GetProperty("id").GetString(),
            otherAbsencesJson.RootElement[0].GetProperty("id").GetString());

        HttpResponseMessage otherMine = await otherMember.GetAsync("/api/me/justificativas");
        Assert.Equal(HttpStatusCode.OK, otherMine.StatusCode);
        using JsonDocument otherMineJson = await JsonDocument.ParseAsync(await otherMine.Content.ReadAsStreamAsync());
        Assert.Empty(otherMineJson.RootElement.EnumerateArray());

        HttpResponseMessage memberDecision = await member.PatchAsJsonAsync(
            $"/api/justificativas/{Guid.NewGuid()}", new { status = "APROVADA" });
        Assert.Equal(HttpStatusCode.Forbidden, memberDecision.StatusCode);
    }

    [Fact]
    public async Task Endpoints_SubmitListApproveRejectAndValidateMultipartContract()
    {
        await using JustificationsApiFactory.Scenario scenario = await _factory.CreateScenarioAsync(twoAbsences: true);
        using HttpClient member = _factory.CreateClientWithToken(scenario.MemberToken);
        using HttpClient director = _factory.CreateClientWithToken(scenario.DirectorToken);

        using MultipartFormDataContent invalidAttachment = CreateForm(
            scenario.FirstAbsenceId, includeAttachment: true, validPdf: false);
        HttpResponseMessage invalid = await member.PostAsync("/api/me/justificativas", invalidAttachment);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using MultipartFormDataContent validForm = CreateForm(scenario.FirstAbsenceId, includeAttachment: true);
        HttpResponseMessage created = await member.PostAsync("/api/me/justificativas", validForm);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/me/justificativas", created.Headers.Location?.ToString());
        using JsonDocument createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        Assert.True(Guid.TryParse(createdJson.RootElement.GetProperty("id").GetString(), out Guid requestId));
        Assert.Equal("EM_ANALISE", createdJson.RootElement.GetProperty("status").GetString());

        using MultipartFormDataContent duplicateForm = CreateForm(scenario.FirstAbsenceId, includeAttachment: false);
        Assert.Equal(HttpStatusCode.Conflict,
            (await member.PostAsync("/api/me/justificativas", duplicateForm)).StatusCode);

        HttpResponseMessage mine = await member.GetAsync("/api/me/justificativas");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        using JsonDocument mineJson = await JsonDocument.ParseAsync(await mine.Content.ReadAsStreamAsync());
        Assert.Single(mineJson.RootElement.EnumerateArray());
        Assert.Equal("EM_ANALISE", mineJson.RootElement[0].GetProperty("status").GetString());

        HttpResponseMessage queue = await director.GetAsync("/api/justificativas");
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        using JsonDocument queueJson = await JsonDocument.ParseAsync(await queue.Content.ReadAsStreamAsync());
        Assert.Contains(queueJson.RootElement.EnumerateArray(), item =>
            item.GetProperty("id").GetString() == requestId.ToString());

        HttpResponseMessage approved = await director.PatchAsJsonAsync(
            $"/api/justificativas/{requestId}", new { status = "APROVADA" });
        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);

        using MultipartFormDataContent secondForm = CreateForm(scenario.SecondAbsenceId, includeAttachment: false);
        HttpResponseMessage secondCreated = await member.PostAsync("/api/me/justificativas", secondForm);
        Assert.Equal(HttpStatusCode.Created, secondCreated.StatusCode);
        using JsonDocument secondCreatedJson = await JsonDocument.ParseAsync(await secondCreated.Content.ReadAsStreamAsync());
        Guid secondRequestId = secondCreatedJson.RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage rejected = await director.PatchAsJsonAsync(
            $"/api/justificativas/{secondRequestId}", new { status = "REJEITADA" });
        Assert.Equal(HttpStatusCode.NoContent, rejected.StatusCode);

        HttpResponseMessage finalMine = await member.GetAsync("/api/me/justificativas");
        using JsonDocument finalMineJson = await JsonDocument.ParseAsync(await finalMine.Content.ReadAsStreamAsync());
        string[] statuses = finalMineJson.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("status").GetString()!)
            .ToArray();
        Assert.Contains("APROVADA", statuses);
        Assert.Contains("REJEITADA", statuses);

    }

    private static MultipartFormDataContent CreateForm(Guid absenceId, bool includeAttachment, bool validPdf = true)
    {
        MultipartFormDataContent form = new();
        form.Add(new StringContent(absenceId.ToString()), "FaltaId");
        form.Add(new StringContent("SAUDE"), "Motivo");
        form.Add(new StringContent(new string('x', 50)), "Descricao");

        if (includeAttachment)
        {
            byte[] bytes = validPdf
                ? "%PDF-1.7\nvalid test attachment"u8.ToArray()
                : "not a PDF"u8.ToArray();
            ByteArrayContent attachment = new(bytes);
            attachment.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(attachment, "Anexo", "comprovante.pdf");
        }

        return form;
    }
}

public sealed class JustificationsApiFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString =
        "Host=localhost;Port=5433;Database=include_presenca_migrations_test;Username=postgres;Password=postgres_test_local";

    private const string JwtKey = "include-presenca-http-tests-key-012345678901234567890123456789";
    private const string JwtIssuer = "include-presenca-http-tests";
    private const string JwtAudience = "include-presenca-http-tests-client";

    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(), "include-presenca-http-tests", Guid.NewGuid().ToString("N"));

    public JustificationsApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("JustificationStorage__RootPath", _storageRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["JustificationStorage:RootPath"] = _storageRoot
            });
        });
    }

    public HttpClient CreateClientWithToken(string token)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<Scenario> CreateScenarioAsync(bool twoAbsences = false)
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        DateTime now = DateTime.UtcNow;
        Member member = new($"HTTP Member {Guid.NewGuid():N}", $"http-member-{Guid.NewGuid():N}@test.local", Role.Membro);
        Member otherMember = new($"HTTP Other {Guid.NewGuid():N}", $"http-other-{Guid.NewGuid():N}@test.local", Role.Membro);
        Member director = new($"HTTP Director {Guid.NewGuid():N}", $"http-director-{Guid.NewGuid():N}@test.local", Role.Diretoria);

        Event firstEvent = CreateAbsenceEvent(now, "HTTP first");
        Event otherEvent = CreateAbsenceEvent(now, "HTTP other");
        EventParticipation firstParticipation = CreateAbsence(firstEvent, member);
        EventParticipation otherParticipation = CreateAbsence(otherEvent, otherMember);

        EventParticipation? secondParticipation = null;
        Event? secondEvent = null;
        if (twoAbsences)
        {
            secondEvent = CreateAbsenceEvent(now, "HTTP second");
            secondParticipation = CreateAbsence(secondEvent, member);
        }

        context.Members.AddRange(member, otherMember, director);
        context.Events.Add(firstEvent);
        context.Events.Add(otherEvent);
        context.EventParticipations.Add(firstParticipation);
        context.EventParticipations.Add(otherParticipation);
        if (secondEvent is not null)
        {
            context.Events.Add(secondEvent);
            context.EventParticipations.Add(secondParticipation!);
        }

        await context.SaveChangesAsync();
        return new Scenario(
            firstParticipation.Id,
            secondParticipation?.Id ?? Guid.Empty,
            CreateToken(member),
            CreateToken(otherMember),
            CreateToken(director));
    }

    private static Event CreateAbsenceEvent(DateTime now, string title) =>
        new(title, null, now.AddHours(-2), now.AddHours(-1));

    private static EventParticipation CreateAbsence(Event @event, Member member)
    {
        EventParticipation participation = new(@event.Id, member.Id);
        participation.ConsolidateUnjustifiedAbsence();
        return participation;
    }

    private static string CreateToken(Member member)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            })
            .Build();
        return new JwtTokenService(configuration).GenerateAccessToken(member);
    }

    public sealed record Scenario(
        Guid FirstAbsenceId,
        Guid SecondAbsenceId,
        string MemberToken,
        string OtherMemberToken,
        string DirectorToken) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal static class HttpClientJsonExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync(
        this HttpClient client, string requestUri, object value) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = JsonContent.Create(value)
        });
}
