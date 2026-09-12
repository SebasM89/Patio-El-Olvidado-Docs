using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>Caja mínima (RN-08). Sin export PDF/CSV (RF-07 fuera de alcance).</summary>
[ApiController]
[Route("api/caja")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class CajaController : ControllerBase
{
    private readonly IPagoService _pagoService;

    public CajaController(IPagoService pagoService) => _pagoService = pagoService;

    [HttpGet("hoy")]
    [ProducesResponseType(typeof(CajaDiaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CajaDiaDto>> GetHoy(CancellationToken cancellationToken)
    {
        var caja = await _pagoService.GetCajaHoyAsync(cancellationToken);
        return Ok(caja);
    }
}
