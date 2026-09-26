using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>
/// Persistencia de Notificaciones. No evalúa umbral de stock ni decide a quién avisar.
/// </summary>
public interface INotificacionRepository
{
    Task<Notificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Avisos del usuario, FechaUtc descendente (IX_Notificaciones_UsuarioId_FechaUtc).
    /// <paramref name="leida"/> null no filtra.
    /// </summary>
    Task<IReadOnlyList<Notificacion>> ListByUsuarioAsync(
        int usuarioId,
        bool? leida,
        CancellationToken cancellationToken = default);

    /// <summary>Cuenta no leídas del usuario (IX_Notificaciones_UsuarioId_NoLeidas).</summary>
    Task<int> CountNoLeidasAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task AddAsync(Notificacion notificacion, CancellationToken cancellationToken = default);
}
