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

    public MembersController(CreateMemberUseCase createMemberUseCase)
    {
        _createMemberUseCase = createMemberUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMemberRequest request)
    {
        Guid memberId = await _createMemberUseCase.ExecuteAsync(request);

        return StatusCode(StatusCodes.Status201Created, new
        {
           Id = memberId
        });
    }
}