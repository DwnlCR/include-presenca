using IncludePresenca.Application.DTOs.Attendance;
using IncludePresenca.Application.Interfaces;
using IncludePresenca.Application.Interfaces.Repositories;
using IncludePresenca.Application.Services;
using IncludePresenca.Domain.Entities;

namespace IncludePresenca.Application.UseCases.Attendance;

public class GenerateEventCheckInCodeUseCase
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private readonly IEventRepository _eventRepository;
    private readonly IEventCheckInCodeRepository _codeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CheckInCodeGenerator _codeGenerator;

    public GenerateEventCheckInCodeUseCase(
        IEventRepository eventRepository,
        IEventCheckInCodeRepository codeRepository,
        IUnitOfWork unitOfWork,
        CheckInCodeGenerator codeGenerator)
    {
        _eventRepository = eventRepository;
        _codeRepository = codeRepository;
        _unitOfWork = unitOfWork;
        _codeGenerator = codeGenerator;
    }

    public async Task<GenerateCheckInCodeResponse> ExecuteAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        Event eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.IsCancelled)
            throw new InvalidOperationException("Cannot generate a code for a cancelled event.");

        string code = _codeGenerator.Generate();
        DateTime createdAtUtc = DateTime.UtcNow;
        DateTime expiresAtUtc = createdAtUtc.Add(CodeLifetime);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _codeRepository.LockEventForCodeGenerationAsync(
                eventId,
                cancellationToken);

            EventCheckInCode? currentCode = await _codeRepository.GetByEventIdAsync(
                eventId,
                forUpdate: true,
                cancellationToken);

            if (currentCode is null)
            {
                await _codeRepository.AddAsync(
                    new EventCheckInCode(
                        eventId,
                        _codeGenerator.Hash(code),
                        createdAtUtc,
                        expiresAtUtc),
                    cancellationToken);
            }
            else
            {
                currentCode.Renew(
                    _codeGenerator.Hash(code),
                    createdAtUtc,
                    expiresAtUtc);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new GenerateCheckInCodeResponse(
            code,
            new DateTimeOffset(expiresAtUtc));
    }
}
