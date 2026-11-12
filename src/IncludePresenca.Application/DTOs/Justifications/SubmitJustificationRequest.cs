namespace IncludePresenca.Application.DTOs.Justifications;

/// <summary>Multipart payload used to submit an absence justification.</summary>
public sealed record SubmitJustificationRequest
{
    /// <summary>Event participation identifier.</summary>
    public required Guid FaltaId { get; init; }

    /// <summary>Reason selected by the member.</summary>
    public required JustificationReasonDto Motivo { get; init; }

    /// <summary>Description of the absence, with at least 50 non-whitespace characters.</summary>
    public required string Descricao { get; init; }

    /// <summary>Optional PDF, PNG or JPEG attachment up to 5 MB.</summary>
    public AttachmentUpload? Anexo { get; init; }
}

/// <summary>Framework-neutral uploaded file data passed from the API layer to storage.</summary>
public sealed record AttachmentUpload(Stream Content, string FileName, string ContentType, long Length);
