using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// Punto de entrada HTTP de las solicitudes de mantenimiento. Enlaza y da forma al contrato, y
/// delega: aquí no vive ninguna regla del ciclo de vida ni acceso a datos.
///
/// <c>[Authorize]</c> solo le cierra la puerta a los llamantes anónimos. Qué rol puede hacer qué,
/// y qué solicitudes puede leer cada uno, son reglas de negocio que los casos de uso aplican
/// contra <c>RequestAccessPolicy</c>, no atributos sobre estos métodos.
/// </summary>
[ApiController]
[Route("api/maintenance-requests")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class MaintenanceRequestsController : ControllerBase
{
    private readonly IMaintenanceRequestService _service;

    public MaintenanceRequestsController(IMaintenanceRequestService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra una solicitud. El solicitante es el usuario autenticado, el estado es siempre
    /// Pending y la fecha la pone el servidor.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> Create(
        [FromBody] CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Lista solicitudes con filtrado, búsqueda, orden y paginación en el servidor. El personal ve
    /// todas; un solicitante, solo las suyas, acotadas por el servicio.
    /// </summary>
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

    /// <summary>Contadores agregados del panel, calculados a partir de los datos persistidos.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(RequestSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestSummaryDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await _service.GetSummaryAsync(cancellationToken));

    /// <summary>Detalle completo, con el responsable y el historial cronológico.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Aplica una transición del ciclo de vida. Rechaza lo que la política no contempla.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.ChangeStatusAsync(id, command, cancellationToken));

    /// <summary>
    /// Resuelve la solicitud con una respuesta para el solicitante. Solo su responsable puede
    /// hacerlo. El endpoint de estado no acepta Resolved, así que ninguna solicitud se cierra sin una.
    /// </summary>
    [HttpPost("{id:guid}/resolution")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> Resolve(
        Guid id,
        [FromBody] ResolveRequestCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.ResolveAsync(id, command, cancellationToken));

    /// <summary>Asigna o reemplaza al responsable. Una solicitud no puede quedar sin responsable.</summary>
    [HttpPatch("{id:guid}/responsible")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> AssignResponsible(
        Guid id,
        [FromBody] AssignResponsibleCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.AssignResponsibleAsync(id, command, cancellationToken));
}
