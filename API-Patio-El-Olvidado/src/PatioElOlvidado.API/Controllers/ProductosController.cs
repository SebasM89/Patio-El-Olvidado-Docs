using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Productos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly IValidator<CreateProductoRequest> _createValidator;
    private readonly IValidator<UpdateProductoRequest> _updateValidator;

    public ProductosController(
        IProductoService productoService,
        IValidator<CreateProductoRequest> createValidator,
        IValidator<UpdateProductoRequest> updateValidator)
    {
        _productoService = productoService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> List(
        [FromQuery] string? q,
        [FromQuery] string? categoria,
        [FromQuery] string? etiqueta,
        [FromQuery] bool? soloActivos,
        CancellationToken cancellationToken)
    {
        var filter = new ProductoFilterQuery
        {
            Q = q,
            Categoria = categoria,
            Etiqueta = etiqueta,
            SoloActivos = soloActivos
        };
        var result = await _productoService.ListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var producto = await _productoService.GetByIdAsync(id, cancellationToken);
        if (producto is null)
            return NotFound(new { message = "Producto no encontrado." });
        return Ok(producto);
    }

    /// <summary>RN-03: solo Admin puede modificar el menú.</summary>
    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductoDto>> Create(
        [FromBody] CreateProductoRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _productoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>RN-03: solo Admin puede modificar el menú.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductoDto>> Update(
        int id,
        [FromBody] UpdateProductoRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _productoService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>RN-03: soft-delete (Activo = false). Solo Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
