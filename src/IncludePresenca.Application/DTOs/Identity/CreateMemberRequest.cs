using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.DTOs.Identity;

public record CreateMemberRequest(string Name, string Email, Role Role);