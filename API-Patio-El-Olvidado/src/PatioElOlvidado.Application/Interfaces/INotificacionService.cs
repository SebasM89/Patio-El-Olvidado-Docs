using PatioElOlvidado.Application.DTOs.Notificaciones;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>
/// Bandeja del usuario autenticado. No crea avisos: eso ocurre en el movimiento de stock (RN-12).
/// </summary>
public interface INotificacionService
{
    Task<IReadOnlyList<NotificacionDto>> ListAsync(
        int usuarioId,
        bool? leida,
        CancellationToken cancellationToken = default);

    Task<NotificacionConteoDto> CountNoLeidasAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<NotificacionDto> MarcarLeidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default);
}
