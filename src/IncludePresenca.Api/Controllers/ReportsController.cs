using IncludePresenca.Application.DTOs.Reports;
using IncludePresenca.Application.Interfaces.Queries;
using IncludePresenca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize(Roles = "Diretoria")]
public sealed class ReportsController : ControllerBase
{
    private readonly IFrequencyReportQueries _queries;
    private readonly FrequencyReportCsvExporter _csvExporter;

    public ReportsController(
        IFrequencyReportQueries queries,
        FrequencyReportCsvExporter csvExporter)
    {
        _queries = queries;
        _csvExporter = csvExporter;
    }

    [HttpGet("frequencia-global")]
    public async Task<ActionResult<GeneralFrequencyReportResponse>> GetGlobalFrequency(
        CancellationToken cancellationToken)
    {
        return Ok(await _queries.GetGeneralReportAsync(cancellationToken));
    }

    [HttpGet("presencas")]
    public async Task<ActionResult<MemberFrequencyReportResponse>> GetAttendances(
        CancellationToken cancellationToken)
    {
        return Ok(await _queries.GetMemberReportAsync(cancellationToken));
    }

    [HttpGet("presencas.csv")]
    public async Task<IActionResult> ExportAttendances(CancellationToken cancellationToken)
    {
        MemberFrequencyReportResponse report = await _queries
            .GetMemberReportAsync(cancellationToken);

        return File(
            _csvExporter.Export(report),
            "text/csv; charset=utf-8",
            "presencas.csv");
    }
}
