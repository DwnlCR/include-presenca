using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Attendance;

public class CheckInEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;
    private readonly IEventCheckInCodeRepository _codeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CheckInCodeValidator _codeValidator;
    private readonly CheckInCodeGenerator _codeGenerator;

    public CheckInEventUseCase(
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository,
        IEventCheckInCodeRepository codeRepository,
        IUnitOfWork unitOfWork,
        CheckInCodeValidator codeValidator,
        CheckInCodeGenerator codeGenerator)
    {
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
        _codeRepository = codeRepository;
        _unitOfWork = unitOfWork;
        _codeValidator = codeValidator;
        _codeGenerator = codeGenerator;
    }

    public async Task<CheckInResponse> ExecuteAsync(
        Guid eventId,
        Guid memberId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        if (memberId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated member is required.");

        if (!_codeValidator.IsValid(code))
            throw new ArgumentException("Invalid check-in code.", nameof(code));

        Event eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.IsCancelled)
            throw new InvalidOperationException("Cannot check in to a cancelled event.");

        DateTime nowUtc = DateTime.UtcNow;

        CheckInResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            EventCheckInCode checkInCode = await _codeRepository.GetByEventIdAsync(
                    eventId,
                    forUpdate: true,
                    cancellationToken)
                ?? throw new InvalidOperationException("Check-in code is not available.");

            if (!checkInCode.IsValid(nowUtc) || !_codeGenerator.Matches(code!, checkInCode.CodeHash))
                throw new ArgumentException("Invalid or expired check-in code.", nameof(code));

            EventParticipation participation = await _participationRepository
                .GetByEventAndMemberIdAsync(
                    eventId,
                    memberId,
                    forUpdate: true,
                    cancellationToken)
                ?? throw new UnauthorizedAccessException("Member is not eligible for this event.");

            participation.RegisterPresence(nowUtc);

            response = new CheckInResponse(
                participation.Id,
                participation.Status.ToString(),
                new DateTimeOffset(nowUtc),
                GetEventAttendanceIsLate(eventEntity, nowUtc));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return response!;
    }

    private static bool GetEventAttendanceIsLate(Event eventEntity, DateTime checkedInAtUtc)
    {
        return checkedInAtUtc > eventEntity.StartsAt.AddMinutes(10);
    }
}
