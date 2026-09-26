using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Inventario;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RN-10: Admin muta el inventario. Empleado consulta. Cliente queda fuera (403).</summary>
[ApiController]
[Route("api/inventario")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _inventario;
    private readonly IValidator<CreateStockItemRequest> _createValidator;
    private readonly IValidator<UpdateStockItemRequest> _updateValidator;
    private readonly IValidator<RegistrarMovimientoRequest> _movimientoValidator;

    public InventarioController(
        IInventarioService inventario,
        IValidator<CreateStockItemRequest> createValidator,
        IValidator<UpdateStockItemRequest> updateValidator,
        IValidator<RegistrarMovimientoRequest> movimientoValidator)
    {
        _inventario = inventario;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _movimientoValidator = movimientoValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? activo,
        [FromQuery] bool? enAlerta,
        CancellationToken cancellationToken)
    {
        var result = await _inventario.ListAsync(
            new StockItemFilterQuery { Q = q, Activo = activo, EnAlerta = enAlerta },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Declarado antes de {id} para que "alertas" no se interprete como identificador.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> Alertas(CancellationToken cancellationToken)
    {
        var result = await _inventario.ListAlertasAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StockItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _inventario.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(new { message = "Ítem de stock no encontrado." });
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StockItemDto>> Create(
        [FromBody] CreateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _inventario.CreateAsync(request, GetActorId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StockItemDto>> Update(
        int id,
        [FromBody] UpdateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _inventario.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:int}/movimientos")]
    [ProducesResponseType(typeof(IReadOnlyList<MovimientoStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MovimientoStockDto>>> ListMovimientos(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _inventario.ListMovimientosAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/movimientos")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(MovimientoStockDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MovimientoStockDto>> RegistrarMovimiento(
        int id,
        [FromBody] RegistrarMovimientoRequest request,
        CancellationToken cancellationToken)
    {
        await _movimientoValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _inventario.RegistrarMovimientoAsync(id, request, GetActorId(), cancellationToken);
        return CreatedAtAction(nameof(ListMovimientos), new { id }, created);
    }

    private int GetActorId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token sin identificador de usuario.");
        if (!int.TryParse(idClaim, out var usuarioId))
            throw new UnauthorizedAccessException("Identificador de usuario inválido.");
        return usuarioId;
    }
}
