using System.Globalization;
using System.Text;
using IncludePresenca.Application.DTOs.Reports;

namespace IncludePresenca.Application.Services;

public sealed class FrequencyReportCsvExporter
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: true);

    public byte[] Export(MemberFrequencyReportResponse report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder csv = new();
        AppendRow(csv, "memberId", "memberName", "presences", "justifiedAbsences", "unjustifiedAbsences", "frequencyPercentage");

        foreach (var member in report.Members)
        {
            AppendRow(
                csv,
                member.MemberId.ToString(),
                member.MemberName,
                member.Counts.Presences.ToString(CultureInfo.InvariantCulture),
                member.Counts.JustifiedAbsences.ToString(CultureInfo.InvariantCulture),
                member.Counts.UnjustifiedAbsences.ToString(CultureInfo.InvariantCulture),
                member.FrequencyPercentage?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return Utf8.GetBytes(csv.ToString());
    }

    private static void AppendRow(StringBuilder csv, params string?[] values)
    {
        csv.AppendJoin(',', values.Select(Escape));
        csv.AppendLine();
    }

    private static string Escape(string? value)
    {
        string safe = value ?? string.Empty;
        string trimmed = safe.TrimStart();

        if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@')
            safe = "'" + safe;

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
