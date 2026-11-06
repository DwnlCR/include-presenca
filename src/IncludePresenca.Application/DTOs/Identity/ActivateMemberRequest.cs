namespace IncludePresenca.Application.DTOs.Identity;

public record ActivateMemberRequest(
    string Token,
    string Password
);