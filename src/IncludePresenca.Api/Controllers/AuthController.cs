
using IncludePresenca.Application.DTOs.Auth;
using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.UseCases.Auth;
using IncludePresenca.Application.UseCases.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ActivateMemberUseCase _activateMemberUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly RequestPasswordResetUseCase _requestPasswordResetUseCase;
    private readonly ResetPasswordUseCase _resetPasswordUseCase;

    public AuthController(
        ActivateMemberUseCase activateMemberUseCase,
        LoginUseCase loginUseCase,
        RequestPasswordResetUseCase requestPasswordResetUseCase,
        ResetPasswordUseCase resetPasswordUseCase)
    {
        _activateMemberUseCase = activateMemberUseCase;
        _loginUseCase = loginUseCase;
        _requestPasswordResetUseCase = requestPasswordResetUseCase;
        _resetPasswordUseCase = resetPasswordUseCase;
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateMemberRequest request)
    {
        await _activateMemberUseCase.ExecuteAsync(request);

        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request)
    {
        LoginResponse response = await _loginUseCase.ExecuteAsync(request);

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] RequestPasswordResetRequest request)
    {
        await _requestPasswordResetUseCase.ExecuteAsync(request);

        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        await _resetPasswordUseCase.ExecuteAsync(request);

        return NoContent();
    }
}
