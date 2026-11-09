using System.Text.Json;
using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.DTOs.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace IncludePresenca.Application.Tests.Serialization;

public class FrequencyReportSerializationTests
{
    private readonly JsonSerializerOptions _options;
    private static readonly Guid MemberId = Guid.Parse("12345678-1234-5678-9abc-123456789abc");
    private static readonly Guid EventId = Guid.Parse("abcdef01-2345-6789-abcd-0123456789ab");

    public FrequencyReportSerializationTests()
    {
        // Program.cs uses AddControllers() without custom JSON configuration.
        // Resolve MVC's actual defaults without starting the API or its database seeder.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        using var provider = services.BuildServiceProvider();
        _options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
            .Value.JsonSerializerOptions;
    }

    private JsonDocument Serialize<T>(T response) =>
        JsonDocument.Parse(JsonSerializer.Serialize(response, _options));

    private static MemberFrequencySummaryResponse Summary(decimal? percentage) =>
        new(MemberId, "Ana", new MemberFrequencyCounts(7, 2, 1), percentage);

    private static void AssertProperties(JsonElement element, params string[] names)
    {
        Assert.Equal(names.OrderBy(name => name),
            element.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Summary_SerializesNullableDecimalWithoutLosingPrecision(bool hasValue)
    {
        decimal? percentage = hasValue ? 700m / 9m : null;
        using var json = Serialize(Summary(percentage));
        JsonElement value = json.RootElement.GetProperty("frequencyPercentage");

        Assert.Equal(hasValue ? JsonValueKind.Number : JsonValueKind.Null, value.ValueKind);
        if (hasValue)
            Assert.Equal(percentage.GetValueOrDefault(), value.GetDecimal());

        var restored = JsonSerializer.Deserialize<MemberFrequencySummaryResponse>(
            json.RootElement.GetRawText(), _options);
        Assert.Equal(percentage, restored!.FrequencyPercentage);
    }

    [Fact]
    public void Summary_SerializesGuidAndCountsWithCamelCaseNames()
    {
        using var json = Serialize(Summary(100m));
        JsonElement root = json.RootElement;

        AssertProperties(root, "memberId", "memberName", "counts", "frequencyPercentage");
        Assert.Equal(MemberId.ToString("D"), root.GetProperty("memberId").GetString());
        Assert.Equal(MemberId, root.GetProperty("memberId").GetGuid());
        Assert.Equal("Ana", root.GetProperty("memberName").GetString());
        JsonElement counts = root.GetProperty("counts");
        AssertProperties(counts, "presences", "unjustifiedAbsences", "justifiedAbsences");
        Assert.Equal(7, counts.GetProperty("presences").GetInt32());
        Assert.Equal(2, counts.GetProperty("unjustifiedAbsences").GetInt32());
        Assert.Equal(1, counts.GetProperty("justifiedAbsences").GetInt32());
    }

    [Theory]
    [InlineData(ParticipationStatus.Present, 1)]
    [InlineData(ParticipationStatus.JustifiedAbsence, 2)]
    [InlineData(ParticipationStatus.UnjustifiedAbsence, 3)]
    public void History_SerializesStatusAsNumberAndDateTimeOffsetAsIso8601(
        ParticipationStatus status, int expectedStatus)
    {
        var date = new DateTimeOffset(2026, 11, 9, 14, 30, 15, TimeSpan.FromHours(-3)).AddTicks(1234567);
        var item = new ParticipationHistoryItemResponse(EventId, "Reunião", date, status, true);
        using var json = Serialize(item);
        JsonElement root = json.RootElement;

        AssertProperties(root, "eventId", "eventName", "eventDate", "status", "isLate");
        Assert.Equal(EventId, root.GetProperty("eventId").GetGuid());
        Assert.Equal("Reunião", root.GetProperty("eventName").GetString());
        Assert.Equal("2026-11-09T14:30:15.1234567-03:00", root.GetProperty("eventDate").GetString());
        DateTimeOffset restoredDate = root.GetProperty("eventDate").GetDateTimeOffset();
        Assert.Equal(date.Ticks, restoredDate.Ticks);
        Assert.Equal(date.Offset, restoredDate.Offset);
        Assert.Equal(JsonValueKind.Number, root.GetProperty("status").ValueKind);
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.True(root.GetProperty("isLate").GetBoolean());
        Assert.Equal(item, JsonSerializer.Deserialize<ParticipationHistoryItemResponse>(
            root.GetRawText(), _options));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneralReport_PreservesLongTotalsAndNullableDecimal(bool hasValue)
    {
        decimal? percentage = hasValue ? 700m / 9m : null;
        var report = new GeneralFrequencyReportResponse(long.MaxValue, 4_294_967_294L, 0L, percentage);
        using var json = Serialize(report);
        JsonElement root = json.RootElement;

        AssertProperties(root, "totalPresences", "totalJustifiedAbsences",
            "totalUnjustifiedAbsences", "globalFrequencyPercentage");
        Assert.Equal(long.MaxValue, root.GetProperty("totalPresences").GetInt64());
        Assert.Equal(4_294_967_294L, root.GetProperty("totalJustifiedAbsences").GetInt64());
        Assert.Equal(0L, root.GetProperty("totalUnjustifiedAbsences").GetInt64());
        JsonElement value = root.GetProperty("globalFrequencyPercentage");
        Assert.Equal(hasValue ? JsonValueKind.Number : JsonValueKind.Null, value.ValueKind);
        if (hasValue)
            Assert.Equal(percentage.GetValueOrDefault(), value.GetDecimal());
        Assert.Equal(report, JsonSerializer.Deserialize<GeneralFrequencyReportResponse>(
            root.GetRawText(), _options));
    }

    [Fact]
    public void Reports_SerializeEmptyCollectionsAsArrays()
    {
        using var members = Serialize(new MemberFrequencyReportResponse([]));
        AssertProperties(members.RootElement, "members");
        Assert.Equal(JsonValueKind.Array, members.RootElement.GetProperty("members").ValueKind);
        Assert.Empty(members.RootElement.GetProperty("members").EnumerateArray());

        using var detail = Serialize(new MemberFrequencyDetailResponse(Summary(null), []));
        AssertProperties(detail.RootElement, "summary", "history");
        Assert.Equal(JsonValueKind.Array, detail.RootElement.GetProperty("history").ValueKind);
        Assert.Empty(detail.RootElement.GetProperty("history").EnumerateArray());
        Assert.Equal(JsonValueKind.Null,
            detail.RootElement.GetProperty("summary").GetProperty("frequencyPercentage").ValueKind);
    }

    [Fact]
    public void Reports_SerializeNestedMembersAndParticipationHistory()
    {
        var summary = Summary(700m / 9m);
        ParticipationHistoryItemResponse[] history =
        [
            new(EventId, "Reunião", DateTimeOffset.Parse("2026-11-09T12:00:00+00:00"), ParticipationStatus.Present, false),
            new(Guid.Empty, "Treinamento", DateTimeOffset.Parse("2026-11-10T12:00:00+00:00"), ParticipationStatus.JustifiedAbsence, false)
        ];
        using var members = Serialize(new MemberFrequencyReportResponse([summary]));
        JsonElement member = Assert.Single(members.RootElement.GetProperty("members").EnumerateArray());
        Assert.Equal(MemberId, member.GetProperty("memberId").GetGuid());
        Assert.Equal(700m / 9m, member.GetProperty("frequencyPercentage").GetDecimal());

        using var detail = Serialize(new MemberFrequencyDetailResponse(summary, history));
        Assert.Equal(MemberId, detail.RootElement.GetProperty("summary").GetProperty("memberId").GetGuid());
        JsonElement items = detail.RootElement.GetProperty("history");
        Assert.Equal(2, items.GetArrayLength());
        Assert.False(items[0].GetProperty("isLate").GetBoolean());
        Assert.Equal(2, items[1].GetProperty("status").GetInt32());
        var restored = JsonSerializer.Deserialize<MemberFrequencyDetailResponse>(
            detail.RootElement.GetRawText(), _options);
        Assert.Equal(summary, restored!.Summary);
        Assert.Equal(history, restored.History.ToArray());
    }
}
