using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// Expone el catálogo del personal para que la interfaz llene el selector de responsable sin
/// fijar identificadores propios. Restringido al personal: un solicitante no necesita la lista
/// y no debe poder enumerar a los empleados de la empresa.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = nameof(UserRole.Staff))]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users)
    {
        _users = users;
    }

    /// <summary>El personal de la empresa, los únicos usuarios que pueden ser responsables de una solicitud.</summary>
    [HttpGet("staff")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetStaff(CancellationToken cancellationToken)
    {
        var staff = await _users.GetByRoleAsync(UserRole.Staff, cancellationToken);

        return Ok(staff.Select(user => new UserDto(user.Id, user.FullName)).ToList());
    }
}
