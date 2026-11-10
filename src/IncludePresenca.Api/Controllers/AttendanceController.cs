using System.Security.Claims;
using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.UseCases.Attendance;
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

    public AttendanceController(
        GetEventAttendancesUseCase getAttendancesUseCase,
        GenerateEventCheckInCodeUseCase generateCodeUseCase,
        CheckInEventUseCase checkInUseCase,
        UpdateEventAttendanceUseCase updateAttendanceUseCase)
    {
        _getAttendancesUseCase = getAttendancesUseCase;
        _generateCodeUseCase = generateCodeUseCase;
        _checkInUseCase = checkInUseCase;
        _updateAttendanceUseCase = updateAttendanceUseCase;
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

    private Guid GetAuthenticatedMemberId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out Guid memberId))
            throw new UnauthorizedAccessException("Authenticated member is required.");

        return memberId;
    }
}
