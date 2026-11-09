using IncludePresenca.Application.Services;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public class CheckInCodeGeneratorTests
{
    private readonly CheckInCodeGenerator _generator = new CheckInCodeGenerator();

    [Fact]
    public void Generate_ReturnsExactlySixAsciiDigits()
    {
        for (int i = 0; i < 1_000; i++)
        {
            string code = _generator.Generate();

            Assert.Equal(6, code.Length);
            Assert.All(code, digit => Assert.InRange(digit, '0', '9'));
        }
    }
}
