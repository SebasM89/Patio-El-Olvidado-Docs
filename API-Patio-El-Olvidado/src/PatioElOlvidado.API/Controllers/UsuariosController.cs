using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>Módulo Usuarios — CRUD solo Admin. Empleado y Cliente reciben 403.</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = RolesSistema.Admin)]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IValidator<CreateUsuarioRequest> _createValidator;
    private readonly IValidator<UpdateUsuarioRequest> _updateValidator;
    private readonly IValidator<CambiarEstadoUsuarioRequest> _estadoValidator;

    public UsuariosController(
        IUsuarioService usuarioService,
        IValidator<CreateUsuarioRequest> createValidator,
        IValidator<UpdateUsuarioRequest> updateValidator,
        IValidator<CambiarEstadoUsuarioRequest> estadoValidator)
    {
        _usuarioService = usuarioService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _estadoValidator = estadoValidator;
    }

    [HttpGet]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> List(
        [FromQuery] string? q,
        [FromQuery] string? rol,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var result = await _usuarioService.ListAsync(
            new UsuarioFilterQuery { Q = q, Rol = rol, Estado = estado },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.GetByIdAsync(id, cancellationToken);
        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });
        return Ok(usuario);
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioDto>> Create(
        [FromBody] CreateUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _usuarioService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioDto>> Update(
        int id,
        [FromBody] UpdateUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _usuarioService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioDto>> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await _estadoValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _usuarioService.CambiarEstadoAsync(id, request, GetActorId(), cancellationToken);
        return Ok(updated);
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
