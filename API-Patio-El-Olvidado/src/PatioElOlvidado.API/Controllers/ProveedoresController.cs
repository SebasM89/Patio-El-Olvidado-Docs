using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Proveedores;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>RN-11: Admin gestiona el catálogo. Empleado consulta. Cliente queda fuera (403).</summary>
[ApiController]
[Route("api/proveedores")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedores;
    private readonly IValidator<CreateProveedorRequest> _createValidator;
    private readonly IValidator<UpdateProveedorRequest> _updateValidator;

    public ProveedoresController(
        IProveedorService proveedores,
        IValidator<CreateProveedorRequest> createValidator,
        IValidator<UpdateProveedorRequest> updateValidator)
    {
        _proveedores = proveedores;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProveedorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ProveedorDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var result = await _proveedores.ListAsync(
            new ProveedorFilterQuery { Q = q, Activo = activo },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProveedorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var proveedor = await _proveedores.GetByIdAsync(id, cancellationToken);
        if (proveedor is null)
            return NotFound(new { message = "Proveedor no encontrado." });
        return Ok(proveedor);
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProveedorDto>> Create(
        [FromBody] CreateProveedorRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _proveedores.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProveedorDto>> Update(
        int id,
        [FromBody] UpdateProveedorRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _proveedores.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}
