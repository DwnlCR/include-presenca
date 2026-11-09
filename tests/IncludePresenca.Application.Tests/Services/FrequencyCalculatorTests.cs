using IncludePresenca.Application.Services;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public class FrequencyCalculatorTests
{
    private readonly FrequencyCalculator _calculator = new FrequencyCalculator();

    [Theory]
    [InlineData(10, 0, 0, 100)]
    [InlineData(0, 5, 0, 0)]
    [InlineData(0, 10, 0, 0)]
    [InlineData(3, 1, 0, 75)]
    [InlineData(3, 1, 100, 75)]
    [InlineData(10, 0, 100, 100)]
    [InlineData(0, 10, 100, 0)]
    [InlineData(1, 7, 0, 12.5)]
    public void Calculate_ReturnsExpectedPercentage(
        int presences, int unjustifiedAbsences, int justifiedAbsences, double expected)
    {
        decimal? result = _calculator.Calculate(presences, unjustifiedAbsences, justifiedAbsences);

        Assert.Equal((decimal)expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(int.MaxValue)]
    public void Calculate_WhenDenominatorIsZero_ReturnsNull(int justifiedAbsences)
    {
        Assert.Null(_calculator.Calculate(0, 0, justifiedAbsences));
    }

    [Theory]
    [InlineData(-1, 0, 0, "presences")]
    [InlineData(0, -1, 0, "unjustifiedAbsences")]
    [InlineData(0, 0, -1, "justifiedAbsences")]
    [InlineData(int.MinValue, 1, 0, "presences")]
    [InlineData(1, int.MinValue, 0, "unjustifiedAbsences")]
    [InlineData(1, 0, int.MinValue, "justifiedAbsences")]
    public void Calculate_WhenQuantityIsNegative_ThrowsArgumentOutOfRangeException(
        int presences, int unjustifiedAbsences, int justifiedAbsences, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => _calculator.Calculate(presences, unjustifiedAbsences, justifiedAbsences));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Calculate_WithJustifiedAbsence_ExcludesItAndPreservesDecimalPrecision()
    {
        decimal? result = _calculator.Calculate(7, 2, 1);

        Assert.Equal(700m / 9m, result);
    }

    [Fact]
    public void Calculate_WithRepeatingDecimal_PreservesDecimalPrecision()
    {
        decimal? result = _calculator.Calculate(2, 1, 0);

        Assert.Equal(66.666666666666666666666666667m, result);
    }

    [Fact]
    public void Calculate_WithMaximumQuantities_DoesNotOverflowIntegerArithmetic()
    {
        decimal? result = _calculator.Calculate(int.MaxValue, int.MaxValue, int.MaxValue);

        Assert.Equal(50m, result);
    }

    [Fact]
    public void Calculate_WithMaximumLongQuantities_DoesNotOverflowIntegerArithmetic()
    {
        decimal? result = _calculator.Calculate(long.MaxValue, long.MaxValue, long.MaxValue);

        Assert.Equal(50m, result);
    }

    [Theory]
    [InlineData(long.MinValue, 0L, 0L)]
    [InlineData(0L, long.MinValue, 0L)]
    [InlineData(0L, 0L, long.MinValue)]
    public void Calculate_WithNegativeLongQuantity_ThrowsArgumentOutOfRangeException(
        long presences, long unjustifiedAbsences, long justifiedAbsences)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _calculator.Calculate(presences, unjustifiedAbsences, justifiedAbsences));
    }
}
