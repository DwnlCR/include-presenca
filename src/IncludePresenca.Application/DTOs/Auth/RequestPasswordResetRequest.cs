
using System.ComponentModel.DataAnnotations;

namespace IncludePresenca.Application.DTOs.Auth;

public record RequestPasswordResetRequest(
    [Required]
    [EmailAddress]
    string Email
);
