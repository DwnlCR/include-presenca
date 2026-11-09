using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.Services;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public class FrequencyAggregatorTests
{
    private readonly FrequencyAggregator _aggregator = new FrequencyAggregator();

    [Fact]
    public void Calculate_WeightsByParticipationInsteadOfAveragingPercentages()
    {
        MemberFrequencyCounts[] members = [new(1, 0, 0), new(0, 9, 0)];

        Assert.Equal(10m, _aggregator.Calculate(members));
    }

    [Fact]
    public void Calculate_ExcludesJustifiedAbsencesAndDoesNotRound()
    {
        MemberFrequencyCounts[] members = [new(3, 1, 100), new(4, 1, 200)];

        Assert.Equal(700m / 9m, _aggregator.Calculate(members));
    }

    [Fact]
    public void Calculate_WithEmptyCollection_ReturnsNull()
    {
        Assert.Null(_aggregator.Calculate(Array.Empty<MemberFrequencyCounts>()));
    }

    [Fact]
    public void Calculate_WithOnlyJustifiedAbsences_ReturnsNull()
    {
        MemberFrequencyCounts[] members = [new(0, 0, int.MaxValue), new(0, 0, int.MaxValue)];

        Assert.Null(_aggregator.Calculate(members));
    }

    [Fact]
    public void Calculate_WithNoParticipation_ReturnsNull()
    {
        MemberFrequencyCounts[] members = [new(0, 0, 0), new(0, 0, 0)];

        Assert.Null(_aggregator.Calculate(members));
    }

    [Theory]
    [InlineData(-1, 0, 0, "Presences")]
    [InlineData(0, -1, 0, "UnjustifiedAbsences")]
    [InlineData(0, 0, -1, "JustifiedAbsences")]
    [InlineData(int.MinValue, 0, 0, "Presences")]
    [InlineData(0, int.MinValue, 0, "UnjustifiedAbsences")]
    [InlineData(0, 0, int.MinValue, "JustifiedAbsences")]
    public void Calculate_WithNegativeQuantity_ThrowsEvenWhenOtherMembersOffsetIt(
        int presences, int unjustifiedAbsences, int justifiedAbsences, string parameterName)
    {
        MemberFrequencyCounts[] members =
        [
            new(int.MaxValue, int.MaxValue, int.MaxValue),
            new(presences, unjustifiedAbsences, justifiedAbsences)
        ];

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _aggregator.Calculate(members));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(int.MaxValue, int.MaxValue, 50)]
    [InlineData(int.MaxValue, 0, 100)]
    [InlineData(0, int.MaxValue, 0)]
    public void Calculate_WithTotalsBeyondInt32_DoesNotOverflow(
        int presences, int unjustifiedAbsences, int expected)
    {
        MemberFrequencyCounts[] members =
        [
            new(presences, unjustifiedAbsences, int.MaxValue),
            new(presences, unjustifiedAbsences, int.MaxValue)
        ];

        Assert.Equal((decimal)expected, _aggregator.Calculate(members));
    }

    [Fact]
    public void Calculate_WithNullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _aggregator.Calculate(null!));
    }
}
