using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>
/// Persistencia de la fila única de HistoriaRestaurante (Id = 1).
/// No inserta ni borra: la semilla vive en SQL.
/// </summary>
public interface IHistoriaRepository
{
    /// <summary>Devuelve la fila Id = 1, o null si la semilla todavía no existe.</summary>
    Task<HistoriaRestaurante?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Marca la fila existente para UPDATE. No hace INSERT.</summary>
    Task UpdateAsync(HistoriaRestaurante historia, CancellationToken cancellationToken = default);
}
