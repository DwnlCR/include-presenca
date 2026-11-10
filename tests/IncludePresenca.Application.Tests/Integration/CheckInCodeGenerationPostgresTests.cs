using System.Text.RegularExpressions;
using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Services;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Infrastructure.Persistence;
using IncludePresenca.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncludePresenca.Application.Tests.Integration;

public sealed class CheckInCodeGenerationPostgresTests
{
    private static readonly Regex SixDigits = new("^[0-9]{6}$", RegexOptions.Compiled);

    [PostgresFact]
    public async Task FirstGeneration_ReturnsSixDigitCodeAndCreatesOneActiveCode()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext setupContext = await CreateDatabaseAsync(connectionString);
        Event eventEntity = await AddEventAsync(setupContext);

        GenerateCheckInCodeResponse response = await GenerateAsync(connectionString, eventEntity);

        Assert.Matches(SixDigits, response.Codigo);
        Assert.Equal(DateTimeKind.Utc, response.ExpiraEmUtc.UtcDateTime.Kind);

        await using AppDbContext assertionContext = CreateContext(connectionString);
        Assert.Equal(1, await assertionContext.EventCheckInCodes.CountAsync(code => code.EventId == eventEntity.Id));
    }

    [PostgresFact]
    public async Task ConcurrentFirstGeneration_DoesNotConflictAndLeavesOneCode()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext setupContext = await CreateDatabaseAsync(connectionString);
        Event eventEntity = await AddEventAsync(setupContext);

        Task<GenerateCheckInCodeResponse> first = GenerateAsync(connectionString, eventEntity);
        Task<GenerateCheckInCodeResponse> second = GenerateAsync(connectionString, eventEntity);
        GenerateCheckInCodeResponse[] responses = await Task.WhenAll(first, second);

        Assert.All(responses, response => Assert.Matches(SixDigits, response.Codigo));

        await using AppDbContext assertionContext = CreateContext(connectionString);
        List<EventCheckInCode> codes = await assertionContext.EventCheckInCodes
            .Where(code => code.EventId == eventEntity.Id)
            .ToListAsync();

        Assert.Single(codes);
        Assert.True(codes[0].IsValid(DateTime.UtcNow));
    }

    [PostgresFact]
    public async Task Renewal_ReplacesCodeAndKeepsOneActiveCode()
    {
        string connectionString = GetConnectionStringOrSkip();
        await using AppDbContext setupContext = await CreateDatabaseAsync(connectionString);
        Event eventEntity = await AddEventAsync(setupContext);

        GenerateCheckInCodeResponse first = await GenerateAsync(connectionString, eventEntity);
        GenerateCheckInCodeResponse renewed = await GenerateAsync(connectionString, eventEntity);

        Assert.Matches(SixDigits, first.Codigo);
        Assert.Matches(SixDigits, renewed.Codigo);

        await using AppDbContext assertionContext = CreateContext(connectionString);
        EventCheckInCode storedCode = await assertionContext.EventCheckInCodes
            .SingleAsync(code => code.EventId == eventEntity.Id);

        Assert.True(new CheckInCodeGenerator().Matches(renewed.Codigo, storedCode.CodeHash));
        Assert.True(storedCode.IsValid(DateTime.UtcNow));
    }

    private static async Task<GenerateCheckInCodeResponse> GenerateAsync(
        string connectionString,
        Event eventEntity)
    {
        await using AppDbContext context = CreateContext(connectionString);
        GenerateEventCheckInCodeUseCase useCase = new(
            new StubEventRepository(eventEntity),
            new EventCheckInCodeRepository(context),
            new UnitOfWork(context),
            new CheckInCodeGenerator());

        return await useCase.ExecuteAsync(eventEntity.Id);
    }

    private static async Task<AppDbContext> CreateDatabaseAsync(string connectionString)
    {
        AppDbContext context = CreateContext(connectionString);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Event> AddEventAsync(AppDbContext context)
    {
        DateTime startsAt = DateTime.UtcNow.AddHours(-1);
        Event eventEntity = new("Check-in concurrency test", null, startsAt, startsAt.AddHours(2));
        await context.Events.AddAsync(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity;
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
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("The PostgreSQL connection string should have been checked by PostgresFact.");

        return connectionString;
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INCLUDE_PRESENCA_TEST_CONNECTION")))
                Skip = "Configure INCLUDE_PRESENCA_TEST_CONNECTION for PostgreSQL integration tests.";
        }
    }

    private sealed class StubEventRepository : IEventRepository
    {
        private readonly Event _eventEntity;

        public StubEventRepository(Event eventEntity)
        {
            _eventEntity = eventEntity;
        }

        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(id == _eventEntity.Id ? _eventEntity : null);

        public Task<List<Event>> GetAllAsync() =>
            Task.FromResult(new List<Event> { _eventEntity });

        public Task AddAsync(Event eventEntity) => Task.CompletedTask;
    }
}
