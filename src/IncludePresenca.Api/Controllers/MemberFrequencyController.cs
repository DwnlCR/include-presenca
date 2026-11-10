using System.Security.Claims;
using IncludePresenca.Application.DTOs.Frequency;
using IncludePresenca.Application.Interfaces.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MemberFrequencyController : ControllerBase
{
    private readonly IFrequencyQueries _queries;

    public MemberFrequencyController(IFrequencyQueries queries)
    {
        _queries = queries;
    }

    [HttpGet("frequencia")]
    public async Task<ActionResult<MemberFrequencyResponse>> GetFrequency(
        CancellationToken cancellationToken)
    {
        MemberFrequencyResponse? response = await _queries
            .GetMemberFrequencyAsync(GetAuthenticatedMemberId(), cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("presencas")]
    public async Task<ActionResult<IReadOnlyList<ParticipationHistoryItemResponse>>> GetHistory(
        [FromQuery(Name = "dias")] int? days,
        CancellationToken cancellationToken)
    {
        return Ok(await _queries.GetMemberHistoryAsync(
            GetAuthenticatedMemberId(), days, cancellationToken));
    }

    private Guid GetAuthenticatedMemberId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out Guid memberId))
            throw new UnauthorizedAccessException("Authenticated member is required.");

        return memberId;
    }
}
