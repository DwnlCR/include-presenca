using IncludePresenca.Application.DTOs.Frequency;

namespace IncludePresenca.Application.Services;

public class FrequencyAggregator
{
    private readonly FrequencyCalculator _calculator = new FrequencyCalculator();

    /// <summary>
    /// Calculates the global percentage from totals, without averaging member percentages.
    /// Throws OverflowException if a total exceeds Int64.MaxValue.
    /// </summary>
    public decimal? Calculate(IEnumerable<MemberFrequencyCounts> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        long presences = 0;
        long unjustifiedAbsences = 0;

        foreach (MemberFrequencyCounts member in members)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(member.Presences, nameof(member.Presences));
            ArgumentOutOfRangeException.ThrowIfNegative(member.UnjustifiedAbsences, nameof(member.UnjustifiedAbsences));
            ArgumentOutOfRangeException.ThrowIfNegative(member.JustifiedAbsences, nameof(member.JustifiedAbsences));

            checked
            {
                presences += member.Presences;
                unjustifiedAbsences += member.UnjustifiedAbsences;
            }
        }

        return _calculator.Calculate(presences, unjustifiedAbsences, 0L);
    }
}
