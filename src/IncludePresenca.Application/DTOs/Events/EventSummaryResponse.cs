namespace IncludePresenca.Application.DTOs.Events;

public record NextEventResponse(
    string Titulo,
    DateTime InicioEm
);

public record EventSummaryResponse(
    NextEventResponse? ProximoEvento,
    int EventosNoMes,
    decimal? PresencaMedia
);