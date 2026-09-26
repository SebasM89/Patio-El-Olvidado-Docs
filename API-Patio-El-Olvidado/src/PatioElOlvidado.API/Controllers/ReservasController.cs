using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Reservas;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>CU08 / RN-06 — reservas básicas de mesa.</summary>
[ApiController]
[Route("api/reservas")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;
    private readonly IValidator<CreateReservaRequest> _createValidator;
    private readonly IValidator<CambiarEstadoReservaRequest> _estadoValidator;

    public ReservasController(
        IReservaService reservaService,
        IValidator<CreateReservaRequest> createValidator,
        IValidator<CambiarEstadoReservaRequest> estadoValidator)
    {
        _reservaService = reservaService;
        _createValidator = createValidator;
        _estadoValidator = estadoValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReservaDto>>> List(
        [FromQuery] DateOnly? fecha,
        [FromQuery] int? mesaId,
        CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var result = await _reservaService.ListAsync(
            new ReservaFilterQuery { Fecha = fecha, MesaId = mesaId },
            usuarioId,
            rol,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservaDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var reserva = await _reservaService.GetByIdAsync(id, usuarioId, rol, cancellationToken);
        if (reserva is null)
            return NotFound(new { message = "Reserva no encontrada." });
        return Ok(reserva);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaDto>> Create(
        [FromBody] CreateReservaRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, rol) = GetActor();
        var created = await _reservaService.CreateAsync(request, usuarioId, rol, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaDto>> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoReservaRequest request,
        CancellationToken cancellationToken)
    {
        await _estadoValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, rol) = GetActor();
        var updated = await _reservaService.CambiarEstadoAsync(id, request, usuarioId, rol, cancellationToken);
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
