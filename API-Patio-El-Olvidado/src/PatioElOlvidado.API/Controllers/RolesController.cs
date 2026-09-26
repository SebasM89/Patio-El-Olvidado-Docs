using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>Catálogo de roles (id, nombre). Solo lectura y solo Admin.</summary>
[ApiController]
[Route("api/roles")]
[Authorize(Roles = RolesSistema.Admin)]
public class RolesController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public RolesController(IUsuarioService usuarioService) => _usuarioService = usuarioService;

    [HttpGet]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<RolCatalogoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RolCatalogoDto>>> List(CancellationToken cancellationToken)
    {
        var result = await _usuarioService.ListRolesAsync(cancellationToken);
        return Ok(result);
    }
}
