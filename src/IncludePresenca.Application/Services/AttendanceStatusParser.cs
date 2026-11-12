using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.Services;

/// <summary>Parses the API's English enum names and the frontend's Portuguese status names.</summary>
public static class AttendanceStatusParser
{
    public static bool TryParseFinal(string? value, out EventParticipationStatus status)
    {
        if (string.Equals(value, nameof(EventParticipationStatus.Present), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "PRESENTE", StringComparison.OrdinalIgnoreCase))
        {
            status = EventParticipationStatus.Present;
            return true;
        }

        if (string.Equals(value, nameof(EventParticipationStatus.JustifiedAbsence), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "FALTA_JUSTIFICADA", StringComparison.OrdinalIgnoreCase))
        {
            status = EventParticipationStatus.JustifiedAbsence;
            return true;
        }

        if (string.Equals(value, nameof(EventParticipationStatus.UnjustifiedAbsence), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "FALTA_NAO_JUSTIFICADA", StringComparison.OrdinalIgnoreCase))
        {
            status = EventParticipationStatus.UnjustifiedAbsence;
            return true;
        }

        status = default;
        return false;
    }
}
