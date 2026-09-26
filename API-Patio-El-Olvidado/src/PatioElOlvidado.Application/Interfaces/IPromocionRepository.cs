using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>
/// Persistencia de Promociones. No aplica descuentos ni decide roles.
/// La baja lógica es Activo = false vía Update; no hay DELETE físico.
/// </summary>
public interface IPromocionRepository
{
    Task<Promocion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="q"/> null o vacío no filtra (Nombre o Descripcion).
    /// <paramref name="activo"/> null no filtra.
    /// <paramref name="vigente"/> null no filtra. true = Activo y la fecha UTC de hoy
    /// inclusive entre VigenteDesde y VigenteHasta. false = el complemento de esa condición.
    /// Orden por Nombre, luego Id.
    /// </summary>
    Task<IReadOnlyList<Promocion>> ListAsync(
        string? q,
        bool? activo,
        bool? vigente,
        CancellationToken cancellationToken = default);

    Task AddAsync(Promocion promocion, CancellationToken cancellationToken = default);
    Task UpdateAsync(Promocion promocion, CancellationToken cancellationToken = default);
}
