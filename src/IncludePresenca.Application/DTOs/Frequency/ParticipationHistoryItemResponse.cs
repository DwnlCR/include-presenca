namespace IncludePresenca.Application.DTOs.Frequency;

/// <summary>
/// EventDate semantics and lateness rules require alignment with the Events module.
/// </summary>
public sealed record ParticipationHistoryItemResponse(
    Guid Id,
    Guid EventoId,
    string Titulo,
    DateTimeOffset InicioEm,
    DateTimeOffset TerminoEm,
    DateTimeOffset? RegistradoEm,
    string Status,
    string? Observacao);
