using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Promociones;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>
/// RN-13: Admin gestiona el catálogo. Cliente consulta activas vigentes.
/// Empleado queda fuera (403). Anónimo, 401.
/// </summary>
[ApiController]
[Route("api/promociones")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Cliente}")]
public class PromocionesController : ControllerBase
{
    private readonly IPromocionService _promociones;
    private readonly IValidator<CreatePromocionRequest> _createValidator;
    private readonly IValidator<UpdatePromocionRequest> _updateValidator;

    public PromocionesController(
        IPromocionService promociones,
        IValidator<CreatePromocionRequest> createValidator,
        IValidator<UpdatePromocionRequest> updateValidator)
    {
        _promociones = promociones;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PromocionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PromocionDto>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? activo,
        [FromQuery] bool? vigente,
        CancellationToken cancellationToken)
    {
        var result = await _promociones.ListAsync(
            new PromocionFilterQuery { Q = q, Activo = activo, Vigente = vigente },
            GetRol(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PromocionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromocionDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var promocion = await _promociones.GetByIdAsync(id, GetRol(), cancellationToken);
        if (promocion is null)
            return NotFound(new { message = "Promoción no encontrada." });
        return Ok(promocion);
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(PromocionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromocionDto>> Create(
        [FromBody] CreatePromocionRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _promociones.CreateAsync(request, GetRol(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(PromocionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromocionDto>> Update(
        int id,
        [FromBody] UpdatePromocionRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _promociones.UpdateAsync(id, request, GetRol(), cancellationToken);
        return Ok(updated);
    }

    /// <summary>Baja lógica (Activo = false). Idempotente: una segunda baja del mismo id responde 204.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _promociones.DeleteAsync(id, GetRol(), cancellationToken);
        return NoContent();
    }

    private string GetRol()
        => User.FindFirstValue(ClaimTypes.Role)
           ?? throw new InvalidOperationException("Token sin rol.");
}
