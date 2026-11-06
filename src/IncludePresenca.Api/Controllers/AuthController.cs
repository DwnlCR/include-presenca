using IncludePresenca.Application.DTOs.Auth;
using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.UseCases.Auth;
using IncludePresenca.Application.UseCases.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ActivateMemberUseCase _activateMemberUseCase;
    private readonly LoginUseCase _loginUseCase;

    public AuthController(ActivateMemberUseCase activateMemberUseCase, LoginUseCase loginUseCase)
    {
        _activateMemberUseCase = activateMemberUseCase;
        _loginUseCase = loginUseCase;
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate(ActivateMemberRequest request)
    {
        await _activateMemberUseCase.ExecuteAsync(request);

        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        LoginResponse response = await _loginUseCase.ExecuteAsync(request);

        return Ok(response);
    }
}