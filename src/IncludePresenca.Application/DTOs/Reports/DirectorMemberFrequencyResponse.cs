namespace IncludePresenca.Application.DTOs.Reports;

/// <summary>Frequency indicators available for a member in the director report.</summary>
public sealed record DirectorMemberFrequencyResponse(
    Guid Id,
    string Nome,
    int Presencas,
    int TotalEventos,
    int Atrasos,
    int Faltas,
    decimal? Frequencia);
