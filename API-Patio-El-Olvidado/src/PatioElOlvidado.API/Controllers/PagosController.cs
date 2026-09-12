using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RF-04 / CU04 — Pagos y división de cuenta; RN-08 actualiza Caja.</summary>
[ApiController]
[Route("api/pagos")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _pagoService;
    private readonly IValidator<CreatePagoRequest> _createValidator;

    public PagosController(
        IPagoService pagoService,
        IValidator<CreatePagoRequest> createValidator)
    {
        _pagoService = pagoService;
        _createValidator = createValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PagoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PagoDto>>> List(
        [FromQuery] int? pedidoId,
        CancellationToken cancellationToken)
    {
        if (!pedidoId.HasValue || pedidoId.Value <= 0)
            return BadRequest(new { message = "pedidoId es obligatorio." });

        var result = await _pagoService.ListByPedidoIdAsync(pedidoId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var pago = await _pagoService.GetByIdAsync(id, cancellationToken);
        if (pago is null)
            return NotFound(new { message = "Pago no encontrado." });
        return Ok(pago);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagoDto>> Create(
        [FromBody] CreatePagoRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _pagoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Anulación solo Admin; revierte montos en Caja (RN-08).</summary>
    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagoDto>> Anular(int id, CancellationToken cancellationToken)
    {
        var anulado = await _pagoService.AnularAsync(id, cancellationToken);
        return Ok(anulado);
    }
}
