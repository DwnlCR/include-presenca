namespace IncludePresenca.Application.DTOs.Frequency;

public readonly record struct MemberFrequencyCounts(
    int Presences,
    int UnjustifiedAbsences,
    int JustifiedAbsences);
