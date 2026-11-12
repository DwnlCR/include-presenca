namespace IncludePresenca.Application.DTOs.Justifications;

/// <summary>Absence available for a member justification request.</summary>
public sealed record PendingAbsenceResponse(Guid Id, string Titulo, DateTimeOffset InicioEm, DateTimeOffset PrazoEm);

/// <summary>Summary of a justification belonging to the authenticated member.</summary>
public sealed record MyJustificationResponse(Guid Id, string Titulo, DateTimeOffset DataFalta, DateTimeOffset EnviadaEm, JustificationStatusDto Status);

/// <summary>Created justification request.</summary>
public sealed record CreatedJustificationResponse(Guid Id, Guid FaltaId, JustificationStatusDto Status, DateTimeOffset EnviadaEm);

/// <summary>Justification request shown in the director queue.</summary>
public sealed record JustificationQueueItemResponse(
    Guid Id, string MembroNome, string? MembroArea, string? MembroCargo, string Titulo,
    DateTimeOffset EventoInicioEm, DateTimeOffset? EventoTerminoEm, bool? EventoObrigatorio,
    DateTimeOffset EnviadaEm, JustificationStatusDto Status, string Motivo, string Descricao,
    string? AnexoNome, bool DentroDoPrazo, decimal? MembroFrequencia,
    int FaltasJustificadas, int FaltasPendentes);

/// <summary>Decision applied by the director to a justification request.</summary>
public sealed record DecideJustificationRequest
{
    /// <summary>Must be APROVADA or REJEITADA.</summary>
    public required JustificationStatusDto Status { get; init; }
}
