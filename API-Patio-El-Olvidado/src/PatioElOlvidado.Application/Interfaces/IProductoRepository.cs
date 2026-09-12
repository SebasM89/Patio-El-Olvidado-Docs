using PatioElOlvidado.Application.DTOs.Productos;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IProductoRepository
{
    Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Producto>> SearchAsync(ProductoFilterQuery filter, CancellationToken cancellationToken = default);
    Task AddAsync(Producto producto, CancellationToken cancellationToken = default);
    Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default);
}
