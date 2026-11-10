using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.Interfaces.Queries;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Queries;

public sealed class FrequencyQueries : IFrequencyQueries
{
    private readonly AppDbContext _context;
    private readonly FrequencyCalculator _calculator;

    public FrequencyQueries(AppDbContext context, FrequencyCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<MemberFrequencyResponse?> GetMemberFrequencyAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        Member? member = await _context.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == memberId, cancellationToken);

        if (member is null)
            return null;

        var rows = await ValidParticipations(memberId)
            .Select(p => new { p.Status, p.CheckedInAtUtc, EventStartsAt = p.Event.StartsAt })
            .ToListAsync(cancellationToken);

        MemberFrequencyCounts counts = Count(rows.Select(row => row.Status));
        int late = rows.Count(row =>
            row.Status == EventParticipationStatus.Present &&
            row.CheckedInAtUtc is not null &&
            row.CheckedInAtUtc > row.EventStartsAt.AddMinutes(10));

        return new MemberFrequencyResponse(
            rows.Count,
            counts.Presences,
            counts.JustifiedAbsences,
            counts.UnjustifiedAbsences,
            late,
            _calculator.Calculate(counts.Presences, counts.UnjustifiedAbsences, counts.JustifiedAbsences));
    }

    public async Task<IReadOnlyList<ParticipationHistoryItemResponse>> GetMemberHistoryAsync(
        Guid memberId,
        int? days = null,
        CancellationToken cancellationToken = default)
    {
        if (days is <= 0)
            throw new ArgumentOutOfRangeException(nameof(days), "The number of days must be greater than zero.");

        DateTime? fromUtc = days.HasValue
            ? DateTime.UtcNow.AddDays(-days.Value)
            : null;

        List<EventParticipation> participations = await ValidParticipations(memberId, fromUtc)
            .Include(p => p.Event)
            .OrderByDescending(p => p.Event.StartsAt)
            .ToListAsync(cancellationToken);

        return participations
            .Select(p =>
            {
                bool isLate = p.Status == EventParticipationStatus.Present &&
                    p.CheckedInAtUtc is not null &&
                    p.CheckedInAtUtc > p.Event.StartsAt.AddMinutes(10);

                return new ParticipationHistoryItemResponse(
                p.Id,
                p.Event.Id,
                p.Event.Title,
                ToUtcOffset(p.Event.StartsAt),
                ToUtcOffset(p.Event.EndsAt),
                p.CheckedInAtUtc is null ? null : ToUtcOffset(p.CheckedInAtUtc.Value),
                ToResponseStatus(p.Status, isLate),
                p.Justification);
            })
            .ToArray();
    }

    internal IQueryable<EventParticipation> ValidParticipations(
        Guid? memberId = null,
        DateTime? fromUtc = null)
    {
        DateTime nowUtc = DateTime.UtcNow;

        IQueryable<EventParticipation> query = _context.EventParticipations
            .AsNoTracking()
            .Where(p => !p.Event.IsCancelled && p.Event.EndsAt <= nowUtc)
            .Where(p => p.Status != EventParticipationStatus.Pending)
            .Where(p => memberId == null || p.MemberId == memberId.Value);

        return fromUtc.HasValue
            ? query.Where(p => p.Event.StartsAt >= fromUtc.Value)
            : query;
    }

    internal static MemberFrequencyCounts Count(IEnumerable<EventParticipationStatus> statuses)
    {
        int presences = statuses.Count(s => s == EventParticipationStatus.Present);
        int justified = statuses.Count(s => s == EventParticipationStatus.JustifiedAbsence);
        int unjustified = statuses.Count(s => s == EventParticipationStatus.UnjustifiedAbsence);
        return new MemberFrequencyCounts(presences, unjustified, justified);
    }

    private static string ToResponseStatus(EventParticipationStatus status, bool isLate) => status switch
    {
        EventParticipationStatus.Present when isLate => ParticipationStatus.Late,
        EventParticipationStatus.Present => ParticipationStatus.Present,
        EventParticipationStatus.JustifiedAbsence => ParticipationStatus.JustifiedAbsence,
        EventParticipationStatus.UnjustifiedAbsence => ParticipationStatus.UnjustifiedAbsence,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Pending is not a classified history status.")
    };

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
