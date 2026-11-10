
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
    private readonly GetMembersUseCase _getMembersUseCase;
    private readonly GetMemberByIdUseCase _getMemberByIdUseCase;
    private readonly DeactivateMemberUseCase _deactivateMemberUseCase;
    private readonly ReactivateMemberUseCase _reactivateMemberUseCase;
    private readonly UpdateMemberUseCase _updateMemberUseCase;

    public MembersController(
        CreateMemberUseCase createMemberUseCase,
        ResendActivationInviteUseCase resendActivationInviteUseCase,
        GetMembersUseCase getMembersUseCase,
        GetMemberByIdUseCase getMemberByIdUseCase,
        DeactivateMemberUseCase deactivateMemberUseCase,
        ReactivateMemberUseCase reactivateMemberUseCase,
        UpdateMemberUseCase updateMemberUseCase)
    {
        _createMemberUseCase = createMemberUseCase;
        _resendActivationInviteUseCase = resendActivationInviteUseCase;
        _getMembersUseCase = getMembersUseCase;
        _getMemberByIdUseCase = getMemberByIdUseCase;
        _deactivateMemberUseCase = deactivateMemberUseCase;
        _reactivateMemberUseCase = reactivateMemberUseCase;
        _updateMemberUseCase = updateMemberUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<List<MemberResponse>>> GetAll()
    {
        List<MemberResponse> members = await _getMembersUseCase.ExecuteAsync();

        return Ok(members);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberResponse>> GetById(Guid id)
    {
        MemberResponse member = await _getMemberByIdUseCase.ExecuteAsync(id);

        return Ok(member);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request)
    {
        CreateMemberResponse response = await _createMemberUseCase.ExecuteAsync(request);

        return Created($"/api/members/{response.Id}", response);
    }

    [HttpPost("{id:guid}/resend-activation")]
    public async Task<IActionResult> ResendActivation(Guid id)
    {
        await _resendActivationInviteUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _deactivateMemberUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await _reactivateMemberUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemberResponse>> Update(
        Guid id, [FromBody] UpdateMemberRequest request
    )
    {
        MemberResponse member = await _updateMemberUseCase.ExecuteAsync(id, request);

        return Ok(member);
    }
}
