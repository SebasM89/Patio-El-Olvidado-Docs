using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Clientes;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RF-05 / CU05 · CU09 · CU10 — Clientes, historial y fidelización.</summary>
[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly IValidator<CreateClienteRequest> _createValidator;
    private readonly IValidator<UpdateClienteRequest> _updateValidator;

    public ClientesController(
        IClienteService clienteService,
        IValidator<CreateClienteRequest> createValidator,
        IValidator<UpdateClienteRequest> updateValidator)
    {
        _clienteService = clienteService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var filter = new ClienteFilterQuery { Q = q, Activo = activo };
        var result = await _clienteService.ListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = RolesSistema.Cliente)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> GetMe(CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var cliente = await _clienteService.GetMeAsync(usuarioId, cancellationToken);
        if (cliente is null)
            return NotFound(new { message = "No hay perfil de cliente vinculado a este usuario." });
        return Ok(cliente);
    }

    [HttpGet("me/historial")]
    [Authorize(Roles = RolesSistema.Cliente)]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialConsumoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistorialConsumoItemDto>>> GetMeHistorial(
        CancellationToken cancellationToken)
    {
        var (usuarioId, _) = GetActor();
        var historial = await _clienteService.GetMeHistorialAsync(usuarioId, cancellationToken);
        return Ok(historial);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var cliente = await _clienteService.GetByIdAsync(id, usuarioId, rol, cancellationToken);
        if (cliente is null)
            return NotFound(new { message = "Cliente no encontrado." });
        return Ok(cliente);
    }

    [HttpGet("{id:int}/historial")]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialConsumoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistorialConsumoItemDto>>> GetHistorial(
        int id,
        CancellationToken cancellationToken)
    {
        var (usuarioId, rol) = GetActor();
        var historial = await _clienteService.GetHistorialAsync(id, usuarioId, rol, cancellationToken);
        return Ok(historial);
    }

    [HttpPost]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClienteDto>> Create(
        [FromBody] CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _clienteService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Update(
        int id,
        [FromBody] UpdateClienteRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _clienteService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Soft-delete (Activo = false). Solo Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _clienteService.DeleteAsync(id, cancellationToken);
        return NoContent();
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
