using System.Security.Claims;
using IncludePresenca.Api.Controllers;
using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IncludePresenca.Application.Tests.Controllers;

public sealed class AttendanceControllerTests
{
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DirectorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task CorrectAttendance_DirectoriaClaimAccessesOperationAndUsesClaimIdentity()
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()),
            new Claim(ClaimTypes.Role, "Diretoria"));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = "unjustifiedabsence",
                AdministrativeReason = "Administrative correction"
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        EventParticipationAudit audit = Assert.Single(fixture.Audits);
        Assert.Equal(DirectorId, audit.ChangedByMemberId);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("UnknownStatus")]
    public async Task CorrectAttendance_NumericOrUnknownStatus_ReturnsBadRequest(
        string status)
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()),
            new Claim(ClaimTypes.Role, "Diretoria"));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = status,
                AdministrativeReason = "Administrative correction"
            },
            CancellationToken.None);

        ProblemDetails problem = Assert.IsType<BadRequestObjectResult>(result).Value as ProblemDetails
            ?? throw new Xunit.Sdk.XunitException("Expected ProblemDetails response.");
        Assert.Equal("Bad Request", problem.Title);
        Assert.Empty(fixture.Audits);
    }

    [Fact]
    public async Task CorrectAttendance_PendingStatus_ReturnsBadRequest()
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = nameof(EventParticipationStatus.Pending),
                AdministrativeReason = "Administrative correction"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(fixture.Audits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CorrectAttendance_MissingOrEmptyReason_ReturnsBadRequest(
        string? administrativeReason)
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = nameof(EventParticipationStatus.UnjustifiedAbsence),
                AdministrativeReason = administrativeReason!
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(fixture.Audits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task CorrectAttendance_MissingOrInvalidIdentityClaim_ReturnsUnauthorized(
        string? claimValue)
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, claimValue ?? string.Empty));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = nameof(EventParticipationStatus.UnjustifiedAbsence),
                AdministrativeReason = "Administrative correction"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Empty(fixture.Audits);
    }

    [Fact]
    public async Task CorrectAttendance_MissingEvent_ReturnsNotFound()
    {
        ControllerFixture fixture = CreateFixture();
        fixture.Events.EventEntity = null;
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            ValidCorrectionRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CorrectAttendance_MissingParticipation_ReturnsNotFound()
    {
        ControllerFixture fixture = CreateFixture();
        fixture.Participations.Participation = null;
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            ValidCorrectionRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CorrectAttendance_SameEffectiveState_ReturnsConflict()
    {
        ControllerFixture fixture = CreateFixture();
        AttendanceController controller = fixture.CreateController(
            new Claim(ClaimTypes.NameIdentifier, DirectorId.ToString()));

        IActionResult result = await controller.CorrectAttendance(
            fixture.Event.Id,
            MemberId,
            new CorrectEventAttendanceRequest
            {
                Status = nameof(EventParticipationStatus.Present),
                AdministrativeReason = "Administrative correction",
                CheckedInAtUtc = new DateTimeOffset(fixture.Participation.CheckedInAtUtc!.Value)
            },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(fixture.Audits);
    }

    [Fact]
    public void CorrectAttendance_EndpointRequiresDiretoriaRole()
    {
        AuthorizeAttribute attribute = Assert.Single(
            typeof(AttendanceController)
                .GetMethod(nameof(AttendanceController.CorrectAttendance))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .OfType<AuthorizeAttribute>());

        Assert.Equal("Diretoria", attribute.Roles);
    }

    [Fact]
    public async Task CorrectAttendance_UnauthenticatedUser_IsChallenged()
    {
        PolicyAuthorizationResult result = await EvaluatePolicyAsync(
            new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.True(result.Challenged);
    }

    [Fact]
    public async Task CorrectAttendance_AuthenticatedNonDirector_IsForbidden()
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.NameIdentifier, MemberId.ToString())],
            authenticationType: "Test");

        PolicyAuthorizationResult result = await EvaluatePolicyAsync(new ClaimsPrincipal(identity));

        Assert.True(result.Forbidden);
    }

    [Fact]
    public void CorrectAttendanceRequest_DoesNotExposeDirectorId()
    {
        Assert.DoesNotContain(
            typeof(CorrectEventAttendanceRequest).GetProperties(),
            property => string.Equals(property.Name, "DirectorId", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<PolicyAuthorizationResult> EvaluatePolicyAsync(
        ClaimsPrincipal user)
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddAuthorizationCore()
            .BuildServiceProvider();
        try
        {
            IPolicyEvaluator evaluator = new PolicyEvaluator(
                services.GetRequiredService<IAuthorizationService>());
            AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole("Diretoria")
                .Build();
            DefaultHttpContext context = new() { User = user };
            AuthenticateResult authentication = await evaluator.AuthenticateAsync(policy, context);

            return await evaluator.AuthorizeAsync(policy, authentication, context, resource: null);
        }
        finally
        {
            await services.DisposeAsync();
        }
    }

    private static ControllerFixture CreateFixture()
    {
        Event eventEntity = new(
            "Controller test event",
            null,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        EventParticipation participation = new(eventEntity.Id, MemberId);
        DateTime checkedInAtUtc = DateTime.UtcNow.AddMinutes(-10);
        long precision = TimeSpan.TicksPerMicrosecond;
        checkedInAtUtc = new DateTime(
            checkedInAtUtc.Ticks - checkedInAtUtc.Ticks % precision,
            DateTimeKind.Utc);
        participation.RegisterPresence(checkedInAtUtc);

        return new ControllerFixture(eventEntity, participation);
    }

    private static CorrectEventAttendanceRequest ValidCorrectionRequest() =>
        new()
        {
            Status = nameof(EventParticipationStatus.UnjustifiedAbsence),
            AdministrativeReason = "Administrative correction"
        };

    private sealed class ControllerFixture
    {
        public ControllerFixture(Event eventEntity, EventParticipation participation)
        {
            Event = eventEntity;
            Participation = participation;
            Events = new FakeEventRepository(eventEntity);
            Participations = new FakeParticipationRepository(participation);
            UseCase = new CorrectEventAttendanceUseCase(
                Events,
                Participations,
                new FakeAuditRepository(Audits),
                new FakeUnitOfWork());
        }

        public Event Event { get; }
        public EventParticipation Participation { get; }
        public List<EventParticipationAudit> Audits { get; } = [];
        public FakeEventRepository Events { get; }
        public FakeParticipationRepository Participations { get; }
        private CorrectEventAttendanceUseCase UseCase { get; }

        public AttendanceController CreateController(params Claim[] claims)
        {
            AttendanceController controller = new(null!, null!, null!, null!, UseCase)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                    }
                }
            };

            return controller;
        }
    }

    private sealed class FakeEventRepository(Event? eventEntity) : IEventRepository
    {
        public Event? EventEntity { get; set; } = eventEntity;

        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(EventEntity is not null && id == EventEntity.Id ? EventEntity : null);

        public Task<List<Event>> GetAllAsync() =>
            Task.FromResult(EventEntity is null
                ? new List<Event>()
                : new List<Event> { EventEntity });

        public Task AddAsync(Event eventEntity) => Task.CompletedTask;
    }

    private sealed class FakeParticipationRepository(EventParticipation? participation)
        : IEventParticipationRepository
    {
        public EventParticipation? Participation { get; set; } = participation;

        public Task AddRangeAsync(List<EventParticipation> participations) =>
            Task.CompletedTask;

        public Task<List<EventParticipation>> GetByEventIdAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Participation is null
                ? new List<EventParticipation>()
                : new List<EventParticipation> { Participation });

        public Task<List<EventParticipation>> GetByEventIdsAsync(
            List<Guid> eventIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Participation is not null && eventIds.Contains(Participation.EventId)
                    ? new List<EventParticipation> { Participation }
                    : new List<EventParticipation>());

        public Task<EventParticipation?> GetByEventAndMemberIdAsync(
            Guid eventId,
            Guid memberId,
            bool forUpdate = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Participation);
    }

    private sealed class FakeAuditRepository(List<EventParticipationAudit> audits)
        : IEventParticipationAuditRepository
    {
        public Task AddAsync(
            EventParticipationAudit audit,
            CancellationToken cancellationToken = default)
        {
            audits.Add(audit);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public async Task ExecuteInTransactionAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default) =>
            await operation();
    }
}
