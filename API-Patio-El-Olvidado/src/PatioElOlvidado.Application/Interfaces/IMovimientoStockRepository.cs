using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>Persistencia append-only de MovimientosStock. No actualiza CantidadActual.</summary>
public interface IMovimientoStockRepository
{
    Task<MovimientoStock?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Historial del ítem, FechaUtc descendente (índice IX_MovimientosStock_StockItemId_FechaUtc).</summary>
    Task<IReadOnlyList<MovimientoStock>> ListByStockItemAsync(
        int stockItemId,
        CancellationToken cancellationToken = default);
    Task AddAsync(MovimientoStock movimiento, CancellationToken cancellationToken = default);
}
