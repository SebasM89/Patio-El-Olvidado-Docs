using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RF-03 / CU03 — Pedidos Local / ParaLlevar.</summary>
[ApiController]
[Route("api/pedidos")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;
    private readonly IValidator<CreatePedidoRequest> _createValidator;
    private readonly IValidator<UpdatePedidoRequest> _updateValidator;
    private readonly IValidator<CambiarEstadoPedidoRequest> _estadoValidator;

    public PedidosController(
        IPedidoService pedidoService,
        IValidator<CreatePedidoRequest> createValidator,
        IValidator<UpdatePedidoRequest> updateValidator,
        IValidator<CambiarEstadoPedidoRequest> estadoValidator)
    {
        _pedidoService = pedidoService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _estadoValidator = estadoValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PedidoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PedidoDto>>> List(
        [FromQuery] string? estado,
        [FromQuery] string? tipo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var filter = new PedidoFilterQuery
        {
            Estado = estado,
            Tipo = tipo,
            Desde = desde,
            Hasta = hasta
        };
        var result = await _pedidoService.ListAsync(filter, usuarioId, rol, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var pedido = await _pedidoService.GetByIdAsync(id, usuarioId, rol, cancellationToken);
        if (pedido is null)
            return NotFound(new { message = "Pedido no encontrado." });
        return Ok(pedido);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PedidoDto>> Create(
        [FromBody] CreatePedidoRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, _) = GetActor();
        var created = await _pedidoService.CreateAsync(request, usuarioId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>RN-04: contenido solo editable en EnPreparacion.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDto>> Update(
        int id,
        [FromBody] UpdatePedidoRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, rol) = GetActor();
        var updated = await _pedidoService.UpdateAsync(id, request, usuarioId, rol, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDto>> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoPedidoRequest request,
        CancellationToken cancellationToken)
    {
        await _estadoValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, rol) = GetActor();
        var updated = await _pedidoService.CambiarEstadoAsync(id, request, usuarioId, rol, cancellationToken);
        return Ok(updated);
    }

    private (int UsuarioId, string Rol) GetActor()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sin NameIdentifier.");
        if (!int.TryParse(idClaim, out var usuarioId))
            throw new InvalidOperationException("NameIdentifier inválido.");

        var rol = User.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Token sin rol.");
        return (usuarioId, rol);
    }
}
