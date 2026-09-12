using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Empleados;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>
/// RF-06 / CU06 — Empleados, fichaje (RN-07) y liquidaciones internas MVP (sin AFIP/PDF).
/// </summary>
[ApiController]
[Route("api/empleados")]
[Authorize]
public class EmpleadosController : ControllerBase
{
    private readonly IEmpleadoService _empleadoService;
    private readonly IValidator<CreateEmpleadoRequest> _createValidator;
    private readonly IValidator<UpdateEmpleadoRequest> _updateValidator;
    private readonly IValidator<CreateLiquidacionRequest> _liquidacionValidator;

    public EmpleadosController(
        IEmpleadoService empleadoService,
        IValidator<CreateEmpleadoRequest> createValidator,
        IValidator<UpdateEmpleadoRequest> updateValidator,
        IValidator<CreateLiquidacionRequest> liquidacionValidator)
    {
        _empleadoService = empleadoService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _liquidacionValidator = liquidacionValidator;
    }

    [HttpGet]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<EmpleadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmpleadoDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var filter = new EmpleadoFilterQuery { Q = q, Activo = activo };
        var result = await _empleadoService.ListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmpleadoDto>> Create(
        [FromBody] CreateEmpleadoRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _empleadoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("me")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpleadoDto>> GetMe(CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var empleado = await _empleadoService.GetMeAsync(usuarioId, cancellationToken);
        if (empleado is null)
            return NotFound(new { message = "No hay perfil de empleado vinculado a este usuario." });
        return Ok(empleado);
    }

    [HttpGet("me/fichajes")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(IReadOnlyList<FichajeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FichajeDto>>> GetMeFichajes(CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var result = await _empleadoService.ListMeFichajesAsync(usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/fichajes/entrada")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(FichajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FichajeDto>> EntradaMe(CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var result = await _empleadoService.RegistrarEntradaMeAsync(usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/fichajes/salida")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(FichajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FichajeDto>> SalidaMe(CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var result = await _empleadoService.RegistrarSalidaMeAsync(usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me/liquidacion/preview")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(LiquidacionPreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LiquidacionPreviewDto>> PreviewMeLiquidacion(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var result = await _empleadoService.PreviewMeLiquidacionAsync(usuarioId, desde, hasta, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me/liquidaciones")]
    [Authorize(Roles = RolesSistema.Empleado)]
    [ProducesResponseType(typeof(IReadOnlyList<LiquidacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LiquidacionDto>>> GetMeLiquidaciones(
        CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var result = await _empleadoService.ListMeLiquidacionesAsync(usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var empleado = await _empleadoService.GetByIdAsync(id, usuarioId, rol, cancellationToken);
        if (empleado is null)
            return NotFound(new { message = "Empleado no encontrado." });
        return Ok(empleado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpleadoDto>> Update(
        int id,
        [FromBody] UpdateEmpleadoRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _empleadoService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Soft-delete (Activo = false).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _empleadoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/fichajes")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<FichajeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FichajeDto>>> GetFichajes(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _empleadoService.ListFichajesAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/fichajes/entrada")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(FichajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FichajeDto>> Entrada(int id, CancellationToken cancellationToken)
    {
        var result = await _empleadoService.RegistrarEntradaAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/fichajes/salida")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(FichajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FichajeDto>> Salida(int id, CancellationToken cancellationToken)
    {
        var result = await _empleadoService.RegistrarSalidaAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/liquidacion/preview")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(LiquidacionPreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LiquidacionPreviewDto>> PreviewLiquidacion(
        int id,
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var result = await _empleadoService.PreviewLiquidacionAsync(id, desde, hasta, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/liquidaciones")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<LiquidacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LiquidacionDto>>> GetLiquidaciones(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _empleadoService.ListLiquidacionesAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Persiste liquidación interna (horas × tarifa). Sin PDF/AFIP.</summary>
    [HttpPost("{id:int}/liquidaciones")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(LiquidacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LiquidacionDto>> GenerarLiquidacion(
        int id,
        [FromBody] CreateLiquidacionRequest request,
        CancellationToken cancellationToken)
    {
        await _liquidacionValidator.ValidateAndThrowAsync(request, cancellationToken);
        var (usuarioId, _) = GetActor();
        var created = await _empleadoService.GenerarLiquidacionAsync(id, request, usuarioId, cancellationToken);
        return CreatedAtAction(nameof(GetLiquidaciones), new { id }, created);
    }

    private (int UsuarioId, string Rol) GetActor()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token sin identificador de usuario.");
        if (!int.TryParse(idClaim, out var usuarioId))
            throw new UnauthorizedAccessException("Identificador de usuario inválido.");

        var rol = User.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Token sin rol.");
        return (usuarioId, rol);
    }
}
