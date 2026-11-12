using System.Security.Claims;
using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Services;
using IncludePresenca.Application.UseCases.Attendance;
using IncludePresenca.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/eventos/{eventoId:guid}")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly GetEventAttendancesUseCase _getAttendancesUseCase;
    private readonly GenerateEventCheckInCodeUseCase _generateCodeUseCase;
    private readonly CheckInEventUseCase _checkInUseCase;
    private readonly UpdateEventAttendanceUseCase _updateAttendanceUseCase;
    private readonly CorrectEventAttendanceUseCase _correctAttendanceUseCase;

    public AttendanceController(
        GetEventAttendancesUseCase getAttendancesUseCase,
        GenerateEventCheckInCodeUseCase generateCodeUseCase,
        CheckInEventUseCase checkInUseCase,
        UpdateEventAttendanceUseCase updateAttendanceUseCase,
        CorrectEventAttendanceUseCase correctAttendanceUseCase)
    {
        _getAttendancesUseCase = getAttendancesUseCase;
        _generateCodeUseCase = generateCodeUseCase;
        _checkInUseCase = checkInUseCase;
        _updateAttendanceUseCase = updateAttendanceUseCase;
        _correctAttendanceUseCase = correctAttendanceUseCase;
    }

    [HttpGet("presencas")]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<List<EventAttendanceResponse>>> GetAttendances(
        Guid eventoId,
        CancellationToken cancellationToken)
    {
        return Ok(await _getAttendancesUseCase.ExecuteAsync(eventoId, cancellationToken));
    }

    [HttpPost("codigos-checkin")]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<GenerateCheckInCodeResponse>> GenerateCode(
        Guid eventoId,
        CancellationToken cancellationToken)
    {
        GenerateCheckInCodeResponse response = await _generateCodeUseCase
            .ExecuteAsync(eventoId, cancellationToken);

        return Ok(response);
    }

    [HttpPost("checkin")]
    [EnableRateLimiting("check-in")]
    public async Task<ActionResult<CheckInResponse>> CheckIn(
        Guid eventoId,
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        Guid memberId = GetAuthenticatedMemberId();
        CheckInResponse response = await _checkInUseCase.ExecuteAsync(
            eventoId,
            memberId,
            request.Value,
            cancellationToken);

        return Ok(response);
    }

    [HttpPatch("presencas/{membroId:guid}")]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<EventAttendanceResponse>> UpdateAttendance(
        Guid eventoId,
        Guid membroId,
        [FromBody] UpdateAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        EventAttendanceResponse response = await _updateAttendanceUseCase.ExecuteAsync(
            eventoId,
            membroId,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPatch("presencas/{membroId:guid}/correcao")]
    [Authorize(Roles = "Diretoria")]
    public async Task<IActionResult> CorrectAttendance(
        Guid eventoId,
        Guid membroId,
        [FromBody] CorrectEventAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedMemberId(out Guid directorId))
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Authenticated director identity is required."
            });

        if (!TryParseFinalStatus(request.Status, out EventParticipationStatus newStatus))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "A final attendance status is required."
            });
        }

        try
        {
            await _correctAttendanceUseCase.ExecuteAsync(
                eventoId,
                membroId,
                directorId,
                newStatus,
                request.AdministrativeReason,
                request.CheckedInAtUtc?.UtcDateTime,
                request.AbsenceJustification,
                request.JustifiedAtUtc?.UtcDateTime,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = "The event participation was not found."
            });
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "The attendance correction is invalid."
            });
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = "The attendance correction conflicts with the current state."
            });
        }
    }

    private Guid GetAuthenticatedMemberId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out Guid memberId))
            throw new UnauthorizedAccessException("Authenticated member is required.");

        return memberId;
    }

    private bool TryGetAuthenticatedMemberId(out Guid memberId)
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out memberId);
    }

    private static bool TryParseFinalStatus(
        string? value,
        out EventParticipationStatus status)
    {
        return AttendanceStatusParser.TryParseFinal(value, out status);
    }
}
