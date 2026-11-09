
using IncludePresenca.Application.DTOs.Identity;
using IncludePresenca.Application.UseCases.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize(Roles = "Diretoria")]
public class MembersController : ControllerBase
{
    private readonly CreateMemberUseCase _createMemberUseCase;
    private readonly ResendActivationInviteUseCase _resendActivationInviteUseCase;

    public MembersController(
        CreateMemberUseCase createMemberUseCase,
        ResendActivationInviteUseCase resendActivationInviteUseCase)
    {
        _createMemberUseCase = createMemberUseCase;
        _resendActivationInviteUseCase = resendActivationInviteUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMemberRequest request)
    {
        CreateMemberResponse response =
            await _createMemberUseCase.ExecuteAsync(request);

        return Created($"/api/members/{response.Id}", response);
    }

    [HttpPost("{id:guid}/resend-activation")]
    public async Task<IActionResult> ResendActivation(Guid id)
    {
        await _resendActivationInviteUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
