using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.DTOs.Identity;

public record MemberResponse(
    Guid Id,
    string Name,
    string Email,
    Role Role,
    StatusConta AccountStatus
);