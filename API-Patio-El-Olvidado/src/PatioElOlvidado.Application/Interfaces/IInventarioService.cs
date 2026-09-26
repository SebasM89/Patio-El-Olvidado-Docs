using PatioElOlvidado.Application.DTOs.Inventario;

namespace PatioElOlvidado.Application.Interfaces;

public interface IInventarioService
{
    Task<IReadOnlyList<StockItemDto>> ListAsync(
        StockItemFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockItemDto>> ListAlertasAsync(CancellationToken cancellationToken = default);

    Task<StockItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<StockItemDto> CreateAsync(
        CreateStockItemRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<StockItemDto> UpdateAsync(
        int id,
        UpdateStockItemRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimientoStockDto>> ListMovimientosAsync(
        int stockItemId,
        CancellationToken cancellationToken = default);

    Task<MovimientoStockDto> RegistrarMovimientoAsync(
        int stockItemId,
        RegistrarMovimientoRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken = default);
}
