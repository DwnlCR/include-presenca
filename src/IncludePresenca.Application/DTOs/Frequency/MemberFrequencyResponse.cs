namespace IncludePresenca.Application.DTOs.Frequency;

/// <summary>Frequency summary for the authenticated member.</summary>
public sealed record MemberFrequencyResponse(
    int TotalEventos,
    int Presencas,
    int FaltasJustificadas,
    int FaltasNaoJustificadas,
    int Atrasos,
    decimal? Percentual);
