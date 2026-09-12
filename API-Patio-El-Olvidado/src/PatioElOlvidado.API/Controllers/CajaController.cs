using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RF-07 / CU07 — Resumen diario de caja + export PDF/CSV. Roles Admin|Empleado (docs/03 lista solo Admin; staff operativo incluido en MVP).</summary>
[ApiController]
[Route("api/caja")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class CajaController : ControllerBase
{
    private readonly ICajaService _cajaService;

    public CajaController(ICajaService cajaService) => _cajaService = cajaService;

    [HttpGet("hoy")]
    [ProducesResponseType(typeof(CajaDiaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CajaDiaDto>> GetHoy(CancellationToken cancellationToken)
    {
        var caja = await _cajaService.GetCajaHoyAsync(cancellationToken);
        return Ok(caja);
    }

    /// <summary>Resumen por fecha UTC. Sin query = hoy UTC. Sin fila = ceros.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CajaDiaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CajaDiaDto>> GetByFecha(
        [FromQuery] DateOnly? fecha,
        CancellationToken cancellationToken)
    {
        var caja = await _cajaService.GetByFechaAsync(fecha, cancellationToken);
        return Ok(caja);
    }

    [HttpGet("export/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] DateOnly? fecha,
        CancellationToken cancellationToken)
    {
        var file = await _cajaService.ExportPdfAsync(fecha, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("export/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateOnly? fecha,
        CancellationToken cancellationToken)
    {
        var file = await _cajaService.ExportCsvAsync(fecha, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
