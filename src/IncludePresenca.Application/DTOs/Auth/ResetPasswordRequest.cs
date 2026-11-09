
using System.ComponentModel.DataAnnotations;

namespace IncludePresenca.Application.DTOs.Auth;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required]
    [MinLength(8)]
    string Password
);
