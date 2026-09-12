using PatioElOlvidado.Application.DTOs.Productos;

namespace PatioElOlvidado.Application.Interfaces;

public interface IProductoService
{
    Task<ProductoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoDto>> ListAsync(ProductoFilterQuery filter, CancellationToken cancellationToken = default);
    Task<ProductoDto> CreateAsync(CreateProductoRequest request, CancellationToken cancellationToken = default);
    Task<ProductoDto> UpdateAsync(int id, UpdateProductoRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
