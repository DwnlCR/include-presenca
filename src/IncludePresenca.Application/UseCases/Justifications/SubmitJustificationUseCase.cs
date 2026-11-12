using IncludePresenca.Application.DTOs.Justifications;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Interfaces.Storage;
using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.UseCases.Justifications;

public sealed class SubmitJustificationUseCase
{
    private const int DeadlineHours = 48;
    private readonly IJustificationParticipationRepository _participations;
    private readonly IJustificationRequestRepository _requests;
    private readonly IPrivateFileStorage _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SubmitJustificationUseCase(
        IJustificationParticipationRepository participations,
        IJustificationRequestRepository requests,
        IPrivateFileStorage storage,
        IUnitOfWork unitOfWork,
        TimeProvider? timeProvider = null)
    {
        _participations = participations;
        _requests = requests;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<CreatedJustificationResponse> ExecuteAsync(
        Guid memberId,
        SubmitJustificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (memberId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated member is required.");
        if (!Enum.IsDefined(request.Motivo))
            throw new ArgumentException("Justification reason is invalid.", nameof(request));

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        StoredPrivateFile? stored = null;
        try
        {
            EventParticipation participation = await _participations.GetByIdAsync(request.FaltaId, false, cancellationToken)
                ?? throw new KeyNotFoundException("Participation not found.");
            if (participation.MemberId != memberId)
                throw new UnauthorizedAccessException("The participation does not belong to the authenticated member.");
            if (participation.Event.IsCancelled)
                throw new InvalidOperationException("Cannot justify a cancelled event.");
            if (participation.Event.EndsAt > now)
                throw new InvalidOperationException("Only completed events can be justified.");
            if (participation.Status != EventParticipationStatus.UnjustifiedAbsence)
                throw new InvalidOperationException("Only an unjustified absence can be justified.");
            if (now > participation.Event.EndsAt.AddHours(DeadlineHours))
                throw new InvalidOperationException("The justification deadline has expired.");
            if (await _requests.ExistsPendingOrApprovedAsync(participation.Id, cancellationToken))
                throw new InvalidOperationException("This participation already has an active justification.");

            if (request.Anexo is not null)
                stored = await _storage.SaveAsync(request.Anexo, cancellationToken);

            JustificationRequest entity = new(
                participation.Id, memberId, ToDomainReason(request.Motivo), request.Descricao,
                now, participation.Event.EndsAt.AddHours(DeadlineHours),
                stored?.StorageKey, stored?.OriginalFileName, stored?.ContentType, stored?.Length);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _requests.AddAsync(entity, cancellationToken);
            }, cancellationToken);

            return new CreatedJustificationResponse(entity.Id, entity.ParticipationId, JustificationStatusDto.EM_ANALISE, new DateTimeOffset(now));
        }
        catch
        {
            if (stored is not null)
            {
                try
                {
                    await _storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
                }
                catch
                {
                    // Preserve the original failure. A cleanup monitor can remove the orphan later.
                }
            }
            throw;
        }
    }

    private static JustificationReason ToDomainReason(JustificationReasonDto reason) => reason switch
    {
        JustificationReasonDto.SAUDE => JustificationReason.Health,
        JustificationReasonDto.TRABALHO_ESTUDOS => JustificationReason.WorkOrStudies,
        JustificationReasonDto.PROBLEMAS_TECNICOS => JustificationReason.TechnicalProblems,
        JustificationReasonDto.OUTRO => JustificationReason.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };
}
