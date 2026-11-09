using IncludePresenca.Domain.Entities;
using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Application.DTOs.Identity;

public record UpdateMemberRequest(string Name, string Email, Role Role);