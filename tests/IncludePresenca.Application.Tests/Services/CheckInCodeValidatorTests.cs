using IncludePresenca.Application.Services;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public class CheckInCodeValidatorTests
{
    private readonly CheckInCodeValidator _validator = new CheckInCodeValidator();

    [Theory]
    [InlineData("000000")]
    [InlineData("000001")]
    [InlineData("012345")]
    [InlineData("123456")]
    [InlineData("999999")]
    public void IsValid_WithSixAsciiDigitsIncludingLeadingZeros_ReturnsTrue(string code)
    {
        Assert.True(_validator.IsValid(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData(" 12345")]
    [InlineData("12345 ")]
    [InlineData("12 345")]
    [InlineData("123456\n")]
    [InlineData("12345\n")]
    [InlineData("12345\t")]
    [InlineData("+12345")]
    [InlineData("-12345")]
    [InlineData("12.345")]
    [InlineData("\u0661\u0662\u0663\u0664\u0665\u0666")]
    [InlineData("\uFF11\uFF12\uFF13\uFF14\uFF15\uFF16")]
    [InlineData("12345\u0666")]
    [InlineData("12345\0")]
    public void IsValid_WithInvalidInput_ReturnsFalse(string? code)
    {
        Assert.False(_validator.IsValid(code));
    }
}
