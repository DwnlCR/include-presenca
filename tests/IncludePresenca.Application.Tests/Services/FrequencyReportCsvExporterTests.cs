using System.Text;
using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.DTOs.Reports;
using IncludePresenca.Application.Services;
using Xunit;

namespace IncludePresenca.Application.Tests.Services;

public sealed class FrequencyReportCsvExporterTests
{
    [Fact]
    public void Export_WithEmptyReport_ContainsOnlyTheHeader()
    {
        string csv = Encoding.UTF8.GetString(
            new FrequencyReportCsvExporter().Export(new MemberFrequencyReportResponse([])));

        string[] lines = csv.TrimStart('\uFEFF').TrimEnd('\r', '\n').Split(Environment.NewLine);

        Assert.Single(lines);
        Assert.Equal(
            "\"memberId\",\"memberName\",\"presences\",\"justifiedAbsences\",\"unjustifiedAbsences\",\"frequencyPercentage\"",
            lines[0]);
    }

    [Fact]
    public void Export_ProducesUtf8CsvWithCoherentHeaders()
    {
        var report = new MemberFrequencyReportResponse([
            new MemberFrequencySummaryResponse(
                Guid.NewGuid(), "Ana", new MemberFrequencyCounts(3, 1, 2), 75m)
        ]);

        string csv = Encoding.UTF8.GetString(new FrequencyReportCsvExporter().Export(report));

        Assert.Contains("\"memberId\",\"memberName\",\"presences\",\"justifiedAbsences\",\"unjustifiedAbsences\",\"frequencyPercentage\"", csv);
        Assert.Contains("\"Ana\"", csv);
        Assert.Contains("\"75\"", csv);
    }

    [Fact]
    public void Export_PreservesJustifiedAbsencesAndWritesNullPercentageAsEmpty()
    {
        var report = new MemberFrequencyReportResponse([
            new MemberFrequencySummaryResponse(
                Guid.NewGuid(), "Ana", new MemberFrequencyCounts(0, 0, 3), null)
        ]);

        string csv = Encoding.UTF8.GetString(new FrequencyReportCsvExporter().Export(report));

        Assert.Contains("\"0\",\"3\",\"0\",\"\"", csv);
    }

    [Theory]
    [InlineData("=cmd|'/C calc'!")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("-10")]
    [InlineData("@malicious")]
    public void Export_PrefixesPotentialCsvFormulaValues(string dangerousName)
    {
        var report = new MemberFrequencyReportResponse([
            new MemberFrequencySummaryResponse(
                Guid.NewGuid(), dangerousName, new MemberFrequencyCounts(1, 0, 0), 100m)
        ]);

        string csv = Encoding.UTF8.GetString(new FrequencyReportCsvExporter().Export(report));

        Assert.Contains($"\"'{dangerousName}\"", csv);
    }

    [Fact]
    public void Export_EscapesQuotesAndCommas()
    {
        var report = new MemberFrequencyReportResponse([
            new MemberFrequencySummaryResponse(
                Guid.NewGuid(), "Silva, \"João\"", new MemberFrequencyCounts(1, 0, 0), 100m)
        ]);

        string csv = Encoding.UTF8.GetString(new FrequencyReportCsvExporter().Export(report));

        Assert.Contains("\"Silva, \"\"João\"\"\"", csv);
    }
}
