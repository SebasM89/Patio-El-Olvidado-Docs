using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Notificaciones;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-12: cada usuario solo ve y marca las notificaciones propias. No hay alta manual ni borrado.
/// </summary>
public class NotificacionService : INotificacionService
{
    private readonly INotificacionRepository _notificaciones;
    private readonly IUnitOfWork _unitOfWork;

    public NotificacionService(INotificacionRepository notificaciones, IUnitOfWork unitOfWork)
    {
        _notificaciones = notificaciones;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificacionDto>> ListAsync(
        int usuarioId,
        bool? leida,
        CancellationToken cancellationToken = default)
    {
        var items = await _notificaciones.ListByUsuarioAsync(usuarioId, leida, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<NotificacionConteoDto> CountNoLeidasAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var cantidad = await _notificaciones.CountNoLeidasAsync(usuarioId, cancellationToken);
        return new NotificacionConteoDto { Cantidad = cantidad };
    }

    public async Task<NotificacionDto> MarcarLeidaAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var notificacion = await _notificaciones.GetByIdAsync(id, cancellationToken);
        if (notificacion is null || notificacion.UsuarioId != usuarioId)
            throw new AppException("Notificación no encontrada.", StatusCodes.Status404NotFound);

        if (!notificacion.Leida)
        {
            notificacion.Leida = true;
            notificacion.LeidaUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Map(notificacion);
    }

    private static NotificacionDto Map(Notificacion notificacion) => new()
    {
        Id = notificacion.Id,
        Titulo = notificacion.Titulo,
        Mensaje = notificacion.Mensaje,
        Tipo = notificacion.Tipo.ToString(),
        Leida = notificacion.Leida,
        FechaUtc = notificacion.FechaUtc,
        LeidaUtc = notificacion.LeidaUtc,
        StockItemId = notificacion.StockItemId,
        MovimientoStockId = notificacion.MovimientoStockId
    };

    private static class StatusCodes
    {
        public const int Status404NotFound = 404;
    }
}
