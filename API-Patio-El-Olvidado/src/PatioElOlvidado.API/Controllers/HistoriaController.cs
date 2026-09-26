using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Historia;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>
/// RN-14: Admin, Empleado y Cliente consultan. Solo Admin edita.
/// Sin alta ni baja. Anónimo, 401.
/// </summary>
[ApiController]
[Route("api/historia")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado},{RolesSistema.Cliente}")]
public class HistoriaController : ControllerBase
{
    private readonly IHistoriaService _historia;
    private readonly IValidator<UpdateHistoriaRequest> _updateValidator;

    public HistoriaController(IHistoriaService historia, IValidator<UpdateHistoriaRequest> updateValidator)
    {
        _historia = historia;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HistoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoriaDto>> Get(CancellationToken cancellationToken)
    {
        var historia = await _historia.GetAsync(GetRol(), cancellationToken);
        if (historia is null)
            return NotFound(new { message = "Historia no configurada" });
        return Ok(historia);
    }

    [HttpPut]
    [Authorize(Roles = RolesSistema.Admin)]
    [ProducesResponseType(typeof(HistoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoriaDto>> Update(
        [FromBody] UpdateHistoriaRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _historia.UpdateAsync(request, GetUsuarioId(), GetRol(), cancellationToken);
        return Ok(updated);
    }

    private string GetRol()
        => User.FindFirstValue(ClaimTypes.Role)
           ?? throw new InvalidOperationException("Token sin rol.");

    private int GetUsuarioId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sin usuario.");
        if (!int.TryParse(idClaim, out var usuarioId))
            throw new InvalidOperationException("Token con usuario inválido.");
        return usuarioId;
    }
}
