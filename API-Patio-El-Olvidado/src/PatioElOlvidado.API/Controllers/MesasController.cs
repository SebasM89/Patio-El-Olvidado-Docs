using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Mesas;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>CU08 — catálogo de mesas (solo lectura).</summary>
[ApiController]
[Route("api/mesas")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
public class MesasController : ControllerBase
{
    private readonly IMesaService _mesaService;

    public MesasController(IMesaService mesaService) => _mesaService = mesaService;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MesaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MesaDto>>> List(CancellationToken cancellationToken)
    {
        var mesas = await _mesaService.ListAsync(cancellationToken);
        return Ok(mesas);
    }
}
