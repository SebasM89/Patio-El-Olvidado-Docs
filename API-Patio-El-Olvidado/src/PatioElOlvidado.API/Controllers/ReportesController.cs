using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Reportes;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>CU11 — Reportes Ventas / Caja rango / Nómina. Solo Admin (docs/03). Fechas UTC.</summary>
[ApiController]
[Route("api/reportes")]
[Authorize(Roles = RolesSistema.Admin)]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportes;
    private readonly IValidator<ReporteRangoQuery> _rangoValidator;

    public ReportesController(
        IReportesService reportes,
        IValidator<ReporteRangoQuery> rangoValidator)
    {
        _reportes = reportes;
        _rangoValidator = rangoValidator;
    }

    [HttpGet("ventas")]
    [ProducesResponseType(typeof(VentasReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VentasReporteDto>> GetVentas(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        return Ok(await _reportes.GetVentasAsync(query, cancellationToken));
    }

    [HttpGet("ventas/export/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportVentasPdf(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportVentasPdfAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("ventas/export/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportVentasCsv(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportVentasCsvAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("caja")]
    [ProducesResponseType(typeof(CajaRangoReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CajaRangoReporteDto>> GetCaja(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        return Ok(await _reportes.GetCajaRangoAsync(query, cancellationToken));
    }

    [HttpGet("caja/export/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCajaPdf(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportCajaRangoPdfAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("caja/export/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCajaCsv(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportCajaRangoCsvAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("nomina")]
    [ProducesResponseType(typeof(NominaReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NominaReporteDto>> GetNomina(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        return Ok(await _reportes.GetNominaAsync(query, cancellationToken));
    }

    [HttpGet("nomina/export/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportNominaPdf(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportNominaPdfAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("nomina/export/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportNominaCsv(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = await ValidateRangoAsync(desde, hasta, cancellationToken);
        var file = await _reportes.ExportNominaCsvAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task<ReporteRangoQuery> ValidateRangoAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var query = new ReporteRangoQuery { Desde = desde, Hasta = hasta };
        await _rangoValidator.ValidateAndThrowAsync(query, cancellationToken);
        return query;
    }
}
