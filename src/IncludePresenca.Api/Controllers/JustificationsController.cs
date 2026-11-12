using System.Security.Claims;
using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.UseCases.Justifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

/// <summary>Endpoints for member justification requests and director decisions.</summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class JustificationsController : ControllerBase
{
    private readonly GetPendingAbsencesUseCase _getPendingAbsences;
    private readonly GetMyJustificationsUseCase _getMine;
    private readonly SubmitJustificationUseCase _submit;
    private readonly GetJustificationQueueUseCase _getQueue;
    private readonly DecideJustificationUseCase _decide;

    public JustificationsController(
        GetPendingAbsencesUseCase getPendingAbsences,
        GetMyJustificationsUseCase getMine,
        SubmitJustificationUseCase submit,
        GetJustificationQueueUseCase getQueue,
        DecideJustificationUseCase decide)
    {
        _getPendingAbsences = getPendingAbsences;
        _getMine = getMine;
        _submit = submit;
        _getQueue = getQueue;
        _decide = decide;
    }

    /// <summary>Lists completed unjustified absences available to the authenticated member.</summary>
    [HttpGet("me/faltas")]
    public async Task<ActionResult<IReadOnlyList<PendingAbsenceResponse>>> GetPendingAbsences(
        [FromQuery] bool? semJustificativa,
        CancellationToken cancellationToken)
    {
        if (semJustificativa is false)
            return Ok(Array.Empty<PendingAbsenceResponse>());
        return Ok(await _getPendingAbsences.ExecuteAsync(GetAuthenticatedMemberId(), cancellationToken));
    }

    /// <summary>Lists justification requests submitted by the authenticated member.</summary>
    [HttpGet("me/justificativas")]
    public async Task<ActionResult<IReadOnlyList<MyJustificationResponse>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _getMine.ExecuteAsync(GetAuthenticatedMemberId(), cancellationToken));
    }

    /// <summary>Submits a justification request with an optional private attachment.</summary>
    [HttpPost("me/justificativas")]
    [RequestSizeLimit(5 * 1024 * 1024 + 64 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreatedJustificationResponse>> Submit(
        [FromForm] SubmitJustificationForm form,
        CancellationToken cancellationToken)
    {
        AttachmentUpload? attachment = null;
        if (form.Anexo is not null)
        {
            await using Stream stream = form.Anexo.OpenReadStream();
            attachment = new AttachmentUpload(stream, form.Anexo.FileName, form.Anexo.ContentType, form.Anexo.Length);
            CreatedJustificationResponse responseWithAttachment = await _submit.ExecuteAsync(
                GetAuthenticatedMemberId(), form.ToRequest(attachment), cancellationToken);
            return Created("/api/me/justificativas", responseWithAttachment);
        }

        SubmitJustificationRequest request = form.ToRequest(null);
        CreatedJustificationResponse response = await _submit.ExecuteAsync(
            GetAuthenticatedMemberId(), request, cancellationToken);
        return Created("/api/me/justificativas", response);
    }

    /// <summary>Lists justification requests for directors to review.</summary>
    [HttpGet("justificativas")]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<IReadOnlyList<JustificationQueueItemResponse>>> GetQueue(CancellationToken cancellationToken)
    {
        return Ok(await _getQueue.ExecuteAsync(cancellationToken));
    }

    /// <summary>Approves or rejects a pending justification request.</summary>
    [HttpPatch("justificativas/{id:guid}")]
    [Authorize(Roles = "Diretoria")]
    public async Task<IActionResult> Decide(
        Guid id,
        [FromBody] DecideJustificationRequest request,
        CancellationToken cancellationToken)
    {
        await _decide.ExecuteAsync(id, GetAuthenticatedMemberId(), request.Status, cancellationToken);
        return NoContent();
    }

    private Guid GetAuthenticatedMemberId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out Guid memberId))
            throw new UnauthorizedAccessException("Authenticated member is required.");
        return memberId;
    }
}

public sealed record SubmitJustificationForm
{
    public required Guid FaltaId { get; init; }
    public required JustificationReasonDto Motivo { get; init; }
    public required string Descricao { get; init; }
    public IFormFile? Anexo { get; init; }

    public SubmitJustificationRequest ToRequest(AttachmentUpload? attachment) => new()
    {
        FaltaId = FaltaId,
        Motivo = Motivo,
        Descricao = Descricao,
        Anexo = attachment
    };
}
