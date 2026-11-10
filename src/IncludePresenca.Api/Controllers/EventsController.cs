
using IncludePresenca.Application.DTOs.Events;
using IncludePresenca.Application.UseCases.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncludePresenca.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly CreateEventUseCase _createEventUseCase;
    private readonly GetEventsUseCase _getEventsUseCase;
    private readonly GetEventByIdUseCase _getEventByIdUseCase;
    private readonly UpdateEventUseCase _updateEventUseCase;
    private readonly CancelEventUseCase _cancelEventUseCase;

    public EventsController(
        CreateEventUseCase createEventUseCase,
        GetEventsUseCase getEventsUseCase,
        GetEventByIdUseCase getEventByIdUseCase,
        UpdateEventUseCase updateEventUseCase,
        CancelEventUseCase cancelEventUseCase)
    {
        _createEventUseCase = createEventUseCase;
        _getEventsUseCase = getEventsUseCase;
        _getEventByIdUseCase = getEventByIdUseCase;
        _updateEventUseCase = updateEventUseCase;
        _cancelEventUseCase = cancelEventUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventResponse>>> GetAll()
    {
        List<EventResponse> events = await _getEventsUseCase.ExecuteAsync();

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventResponse>> GetById(Guid id)
    {
        EventResponse eventResponse = await _getEventByIdUseCase.ExecuteAsync(id);

        return Ok(eventResponse);
    }

    [HttpPost]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<EventResponse>> Create(
        [FromBody] CreateEventRequest request)
    {
        EventResponse response = await _createEventUseCase.ExecuteAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response
        );
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Diretoria")]
    public async Task<ActionResult<EventResponse>> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        EventResponse response = await _updateEventUseCase.ExecuteAsync(id, request);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Diretoria")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _cancelEventUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
