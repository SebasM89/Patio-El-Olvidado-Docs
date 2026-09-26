using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>Persistencia de StockItems. Sin reglas de stock ni semilla.</summary>
public interface IStockItemRepository
{
    Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StockItem?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    /// <summary><paramref name="activo"/> null no filtra. Orden por Nombre.</summary>
    Task<IReadOnlyList<StockItem>> ListAsync(bool? activo, CancellationToken cancellationToken = default);
    Task AddAsync(StockItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockItem item, CancellationToken cancellationToken = default);
}
