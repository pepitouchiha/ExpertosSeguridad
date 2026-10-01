using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// Endpoints de inicio de sesión. Todo lo demás en la API requiere el token emitido aquí.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Intercambia correo y contraseña por un token de acceso.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(command, cancellationToken));

    /// <summary>
    /// Registro público de clientes. La cuenta siempre se crea como solicitante —el cuerpo no tiene
    /// campo de rol— y queda con la sesión iniciada de inmediato.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResultDto>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(command, cancellationToken);

        return CreatedAtAction(nameof(Me), null, result);
    }

    /// <summary>Devuelve el usuario de la sesión, para que un cliente con token pueda restaurarla.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await _authService.GetCurrentUserAsync(cancellationToken));
}
