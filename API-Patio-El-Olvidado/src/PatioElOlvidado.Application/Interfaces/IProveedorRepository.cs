using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>Persistencia de Proveedores. Sin reglas de reposición ni de stock.</summary>
public interface IProveedorRepository
{
    Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary><paramref name="activo"/> null no filtra. Orden por Nombre.</summary>
    Task<IReadOnlyList<Proveedor>> ListAsync(bool? activo, CancellationToken cancellationToken = default);

    Task AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default);
}
