namespace IncludePresenca.Application.Services;

public class FrequencyCalculator
{

    public decimal? Calculate(int presences, int unjustifiedAbsences, int justifiedAbsences)
    {
        return Calculate((long)presences, unjustifiedAbsences, justifiedAbsences);
    }

    public decimal? Calculate(long presences, long unjustifiedAbsences, long justifiedAbsences)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(presences);
        ArgumentOutOfRangeException.ThrowIfNegative(unjustifiedAbsences);
        ArgumentOutOfRangeException.ThrowIfNegative(justifiedAbsences);

        decimal denominator = (decimal)presences + unjustifiedAbsences;

        if (denominator == 0)
        {
            return null;
        }

        return presences * 100m / denominator;
    }
}
