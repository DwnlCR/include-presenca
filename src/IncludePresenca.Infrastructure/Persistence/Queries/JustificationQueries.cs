using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces.Queries;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Queries;

public sealed class JustificationQueries : IJustificationQueries
{
    private const int DeadlineHours = 48;
    private readonly AppDbContext _context;
    private readonly FrequencyCalculator _calculator;

    public JustificationQueries(AppDbContext context, FrequencyCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<IReadOnlyList<PendingAbsenceResponse>> GetPendingAbsencesAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        return await _context.EventParticipations.AsNoTracking()
            .Where(p => p.MemberId == memberId && p.Status == EventParticipationStatus.UnjustifiedAbsence &&
                !p.Event.IsCancelled && p.Event.EndsAt <= now &&
                !_context.JustificationRequests.Any(j => j.ParticipationId == p.Id &&
                    (j.Status == JustificationRequestStatus.Pending || j.Status == JustificationRequestStatus.Approved)))
            .OrderByDescending(p => p.Event.StartsAt)
            .Select(p => new PendingAbsenceResponse(
                p.Id,
                p.Event.Title,
                ToOffset(p.Event.StartsAt),
                ToOffset(p.Event.EndsAt.AddHours(DeadlineHours))))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MyJustificationResponse>> GetMineAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.JustificationRequests.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new MyJustificationResponse(
                x.Id, x.Participation.Event.Title, ToOffset(x.Participation.Event.StartsAt),
                ToOffset(x.SubmittedAtUtc), ToDtoStatus(x.Status)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JustificationQueueItemResponse>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        var requests = await _context.JustificationRequests.AsNoTracking()
            .Include(x => x.Participation).ThenInclude(x => x.Event)
            .Include(x => x.Member)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var memberIds = requests.Select(x => x.MemberId).Distinct().ToArray();
        var rows = await _context.EventParticipations.AsNoTracking()
            .Where(x => memberIds.Contains(x.MemberId) && !x.Event.IsCancelled && x.Event.EndsAt <= now && x.Status != EventParticipationStatus.Pending)
            .Select(x => new { x.MemberId, x.Status })
            .ToListAsync(cancellationToken);

        return requests.Select(x =>
        {
            var memberRows = rows.Where(r => r.MemberId == x.MemberId).Select(r => r.Status).ToArray();
            int justified = memberRows.Count(s => s == EventParticipationStatus.JustifiedAbsence);
            int pending = memberRows.Count(s => s == EventParticipationStatus.UnjustifiedAbsence);
            int present = memberRows.Count(s => s == EventParticipationStatus.Present);
            int unjustified = pending;
            decimal? frequency = _calculator.Calculate(present, unjustified, justified);
            return new JustificationQueueItemResponse(
                x.Id, x.Member.Name, null, null, x.Participation.Event.Title,
                ToOffset(x.Participation.Event.StartsAt), ToOffset(x.Participation.Event.EndsAt), null,
                ToOffset(x.SubmittedAtUtc), ToDtoStatus(x.Status), ToDtoReason(x.Reason), x.Description,
                x.AttachmentOriginalFileName, x.SubmittedAtUtc <= x.DeadlineUtc,
                frequency, justified, pending);
        }).ToArray();
    }

    private static JustificationStatusDto ToDtoStatus(JustificationRequestStatus status) => status switch
    {
        JustificationRequestStatus.Pending => JustificationStatusDto.EM_ANALISE,
        JustificationRequestStatus.Approved => JustificationStatusDto.APROVADA,
        JustificationRequestStatus.Rejected => JustificationStatusDto.REJEITADA,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string ToDtoReason(JustificationReason reason) => reason switch
    {
        JustificationReason.Health => "SAUDE",
        JustificationReason.WorkOrStudies => "TRABALHO_ESTUDOS",
        JustificationReason.TechnicalProblems => "PROBLEMAS_TECNICOS",
        JustificationReason.Other => "OUTRO",
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
