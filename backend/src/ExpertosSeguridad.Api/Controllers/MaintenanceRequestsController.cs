using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// HTTP entry point for the maintenance requests. It binds and shapes the contract, then
/// delegates: no lifecycle rule and no data access lives here.
/// </summary>
[ApiController]
[Route("api/maintenance-requests")]
[Produces("application/json")]
public sealed class MaintenanceRequestsController : ControllerBase
{
    private readonly IMaintenanceRequestService _service;

    public MaintenanceRequestsController(IMaintenanceRequestService service)
    {
        _service = service;
    }

    /// <summary>Registers a request. The status is always Pending and the date comes from the server.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> Create(
        [FromBody] CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Lists requests with server-side filtering, search, sorting and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MaintenanceRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MaintenanceRequestListItemDto>>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = MaintenanceRequestQuery.DefaultPageSize,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestPriority? priority = null,
        [FromQuery] RequestCategory? category = null,
        [FromQuery] string? search = null,
        [FromQuery] SortDirection sortByCreatedAt = SortDirection.Desc,
        CancellationToken cancellationToken = default)
    {
        var query = new MaintenanceRequestQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            Priority = priority,
            Category = category,
            Search = search,
            SortByCreatedAt = sortByCreatedAt
        };

        return Ok(await _service.SearchAsync(query, cancellationToken));
    }

    /// <summary>Aggregated counters for the dashboard, computed from persisted data.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(RequestSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestSummaryDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await _service.GetSummaryAsync(cancellationToken));

    /// <summary>Full detail, including the responsible and the chronological history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Applies a lifecycle transition. Rejects anything outside the transition policy.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.ChangeStatusAsync(id, command, cancellationToken));

    /// <summary>Assigns or replaces the responsible. A null identifier clears the assignment.</summary>
    [HttpPatch("{id:guid}/responsible")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> AssignResponsible(
        Guid id,
        [FromBody] AssignResponsibleCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.AssignResponsibleAsync(id, command, cancellationToken));
}
