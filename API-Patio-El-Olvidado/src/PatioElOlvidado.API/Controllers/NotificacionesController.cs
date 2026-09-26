using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatioElOlvidado.Application.DTOs.Notificaciones;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.API.Controllers;

/// <summary>
/// RN-12: bandeja in-app. Admin y Empleado consultan y marcan las propias. Cliente queda fuera (403).
/// El id del destinatario sale del claim; no hay alta manual ni borrado.
/// </summary>
[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = $"{RolesSistema.Admin},{RolesSistema.Empleado}")]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionService _notificaciones;

    public NotificacionesController(INotificacionService notificaciones) => _notificaciones = notificaciones;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> List(
        [FromQuery] bool? leida,
        CancellationToken cancellationToken)
    {
        var result = await _notificaciones.ListAsync(GetActorId(), leida, cancellationToken);
        return Ok(result);
    }

    /// <summary>Ruta estática antes de {id} para que "conteo" no se interprete como identificador.</summary>
    [HttpGet("conteo")]
    [ProducesResponseType(typeof(NotificacionConteoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificacionConteoDto>> Conteo(CancellationToken cancellationToken)
    {
        var result = await _notificaciones.CountNoLeidasAsync(GetActorId(), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:int}/leida")]
    [ProducesResponseType(typeof(NotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificacionDto>> MarcarLeida(int id, CancellationToken cancellationToken)
    {
        var updated = await _notificaciones.MarcarLeidaAsync(id, GetActorId(), cancellationToken);
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
