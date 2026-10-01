using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// Endpoints del panel de administración. El atributo de rol es el filtro grueso; los casos de
/// uso vuelven a verificar <c>UserAdministrationPolicy</c>, y la entidad rechaza los cambios que
/// un administrador intente hacer sobre su propia cuenta.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(Roles = nameof(UserRole.Admin))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserAdministrationService _service;

    public AdminUsersController(IUserAdministrationService service)
    {
        _service = service;
    }

    /// <summary>Lista paginada de cuentas, filtrable por rol, estado y nombre o correo.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = UserQuery.DefaultPageSize,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new UserQuery
        {
            Page = page,
            PageSize = pageSize,
            Role = role,
            IsActive = isActive,
            Search = search
        };

        return Ok(await _service.SearchAsync(query, cancellationToken));
    }

    /// <summary>Asigna un rol nuevo. Un administrador no puede cambiar el suyo.</summary>
    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.ChangeRoleAsync(id, command, cancellationToken));

    /// <summary>Activa o desactiva una cuenta. Un administrador no puede desactivar la suya.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeUserStatusCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _service.ChangeStatusAsync(id, command, cancellationToken));
}
