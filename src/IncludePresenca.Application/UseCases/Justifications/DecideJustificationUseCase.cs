using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Justifications;

public sealed class DecideJustificationUseCase
{
    private readonly IJustificationRequestRepository _requests;
    private readonly IJustificationParticipationRepository _participations;
    private readonly IJustificationAuditRepository _audits;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public DecideJustificationUseCase(
        IJustificationRequestRepository requests,
        IJustificationParticipationRepository participations,
        IJustificationAuditRepository audits,
        IUnitOfWork unitOfWork,
        TimeProvider? timeProvider = null)
    {
        _requests = requests;
        _participations = participations;
        _audits = audits;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task ExecuteAsync(Guid requestId, Guid directorId, JustificationStatusDto decision, CancellationToken cancellationToken = default)
    {
        if (directorId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated director is required.");
        if (decision is not (JustificationStatusDto.APROVADA or JustificationStatusDto.REJEITADA))
            throw new ArgumentException("Decision must be APROVADA or REJEITADA.", nameof(decision));

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            JustificationRequest request = await _requests.GetByIdAsync(requestId, true, cancellationToken)
                ?? throw new KeyNotFoundException("Justification request not found.");
            EventParticipation participation = await _participations.GetByIdAsync(request.ParticipationId, true, cancellationToken)
                ?? throw new KeyNotFoundException("Participation not found.");
            if (participation.Event.IsCancelled || participation.Event.EndsAt > _timeProvider.GetUtcNow().UtcDateTime)
                throw new InvalidOperationException("The participation is not eligible for a decision.");
            if (request.Status != JustificationRequestStatus.Pending)
                throw new InvalidOperationException("Justification request was already decided.");
            if (decision == JustificationStatusDto.APROVADA && participation.Status != EventParticipationStatus.UnjustifiedAbsence)
                throw new InvalidOperationException("The participation is no longer an unjustified absence.");

            JustificationRequestStatus previousRequestStatus = request.Status;
            EventParticipationStatus previousParticipationStatus = participation.Status;
            DateTime decidedAt = _timeProvider.GetUtcNow().UtcDateTime;

            if (decision == JustificationStatusDto.APROVADA)
            {
                request.Approve(directorId, decidedAt);
                participation.ApplyApprovedJustification(request.Description, decidedAt);
            }
            else
            {
                request.Reject(directorId, decidedAt);
            }

            await _audits.AddAsync(new JustificationAudit(
                request, directorId, decidedAt, previousRequestStatus,
                previousParticipationStatus, participation.Status), cancellationToken);
        }, cancellationToken);
    }
}
