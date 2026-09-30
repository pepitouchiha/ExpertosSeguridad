using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Controllers;

/// <summary>
/// Exposes the fixed catalogue of test users so the UI can offer requesters, responsibles
/// and the acting user without hardcoding identifiers of its own.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserDirectory _userDirectory;

    public UsersController(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<UserDto>> GetAll() =>
        Ok(_userDirectory.GetAll().Select(user => new UserDto(user.Id, user.Name)).ToList());
}
