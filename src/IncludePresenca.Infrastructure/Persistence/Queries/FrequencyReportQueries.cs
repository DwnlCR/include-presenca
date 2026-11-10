using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.DTOs.Reports;
using IncludePresenca.Application.Interfaces.Queries;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncludePresenca.Infrastructure.Persistence.Queries;

public sealed class FrequencyReportQueries : IFrequencyReportQueries
{
    private const decimal DirectorTargetPercentage = 75m;
    private readonly AppDbContext _context;
    private readonly FrequencyQueries _frequencyQueries;
    private readonly FrequencyCalculator _calculator;
    private readonly FrequencyAggregator _aggregator;

    public FrequencyReportQueries(
        AppDbContext context,
        FrequencyQueries frequencyQueries,
        FrequencyCalculator calculator,
        FrequencyAggregator aggregator)
    {
        _context = context;
        _frequencyQueries = frequencyQueries;
        _calculator = calculator;
        _aggregator = aggregator;
    }

    public async Task<GeneralFrequencyReportResponse> GetGeneralReportAsync(
        CancellationToken cancellationToken = default)
    {
        List<ReportParticipationRow> rows = await _frequencyQueries
            .ValidParticipations()
            .Select(p => new ReportParticipationRow(
                p.MemberId,
                p.Status,
                p.CheckedInAtUtc,
                p.Event.StartsAt))
            .ToListAsync(cancellationToken);

        MemberFrequencyCounts counts = FrequencyQueries.Count(rows.Select(row => row.Status));
        decimal? percentage = _aggregator.Calculate([counts]);
        List<Member> activeMembers = await _context.Members
            .AsNoTracking()
            .Where(member => member.AccountStatus == StatusConta.Ativa)
            .OrderBy(member => member.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<DirectorMemberFrequencyResponse> memberReports = activeMembers
            .Select(member => CreateDirectorSummary(
                member,
                rows.Where(row => row.MemberId == member.Id)))
            .ToArray();

        GeneralFrequencyReportResponse response = new(
            counts.Presences,
            counts.JustifiedAbsences,
            counts.UnjustifiedAbsences,
            percentage)
        {
            MembrosAtivos = activeMembers.Count,
            AbaixoDaMeta = memberReports.Count(member =>
                member.Frequencia is null || member.Frequencia < DirectorTargetPercentage),
            Membros = memberReports
        };
        return response;
    }

    public async Task<MemberFrequencyReportResponse> GetMemberReportAsync(
        CancellationToken cancellationToken = default)
    {
        List<Member> members = await _context.Members
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        var grouped = await _frequencyQueries.ValidParticipations()
            .Select(p => new { p.MemberId, p.Status })
            .ToListAsync(cancellationToken);

        IReadOnlyList<MemberFrequencySummaryResponse> summaries = members
            .Select(member => CreateSummary(
                member,
                grouped.Where(p => p.MemberId == member.Id).Select(p => p.Status)))
            .ToArray();

        return new MemberFrequencyReportResponse(summaries);
    }

    public async Task<MemberFrequencyDetailResponse?> GetMemberDetailAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        MemberFrequencyResponse? memberFrequency = await _frequencyQueries
            .GetMemberFrequencyAsync(memberId, cancellationToken);

        if (memberFrequency is null)
            return null;

        Member? member = await _context.Members
            .AsNoTracking()
            .SingleAsync(m => m.Id == memberId, cancellationToken);

        MemberFrequencyCounts counts = new(
            memberFrequency.Presencas,
            memberFrequency.FaltasNaoJustificadas,
            memberFrequency.FaltasJustificadas);

        MemberFrequencySummaryResponse summary = new(
            member.Id,
            member.Name,
            counts,
            memberFrequency.Percentual);

        IReadOnlyList<ParticipationHistoryItemResponse> history = await _frequencyQueries
            .GetMemberHistoryAsync(memberId, cancellationToken: cancellationToken);

        return new MemberFrequencyDetailResponse(summary, history);
    }

    private MemberFrequencySummaryResponse CreateSummary(
        Member member, IEnumerable<EventParticipationStatus> statuses)
    {
        MemberFrequencyCounts counts = FrequencyQueries.Count(statuses);
        return new MemberFrequencySummaryResponse(
            member.Id,
            member.Name,
            counts,
            _calculator.Calculate(counts.Presences, counts.UnjustifiedAbsences, counts.JustifiedAbsences));
    }

    private DirectorMemberFrequencyResponse CreateDirectorSummary(
        Member member,
        IEnumerable<ReportParticipationRow> rows)
    {
        List<ReportParticipationRow> materializedRows = rows.ToList();
        MemberFrequencyCounts counts = FrequencyQueries.Count(materializedRows.Select(row => row.Status));
        int late = materializedRows.Count(row =>
            row.Status == EventParticipationStatus.Present &&
            row.CheckedInAtUtc is not null &&
            row.CheckedInAtUtc > row.EventStartsAt.AddMinutes(10));

        return new DirectorMemberFrequencyResponse(
            member.Id,
            member.Name,
            counts.Presences,
            materializedRows.Count,
            late,
            counts.JustifiedAbsences + counts.UnjustifiedAbsences,
            _calculator.Calculate(counts.Presences, counts.UnjustifiedAbsences, counts.JustifiedAbsences));
    }

    private sealed record ReportParticipationRow(
        Guid MemberId,
        EventParticipationStatus Status,
        DateTime? CheckedInAtUtc,
        DateTime EventStartsAt);
}
