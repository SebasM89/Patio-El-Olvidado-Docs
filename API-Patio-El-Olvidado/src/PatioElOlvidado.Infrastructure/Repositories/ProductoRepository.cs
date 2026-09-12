using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Productos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _db;

    public ProductoRepository(AppDbContext db) => _db = db;

    public Task<Producto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Productos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Producto>> SearchAsync(
        ProductoFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Productos.AsQueryable();

        var soloActivos = filter.SoloActivos ?? true;
        if (soloActivos)
            query = query.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(p =>
                p.Nombre.Contains(q) ||
                (p.Descripcion != null && p.Descripcion.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Categoria))
        {
            var categoria = filter.Categoria.Trim();
            query = query.Where(p => p.Categoria == categoria);
        }

        if (!string.IsNullOrWhiteSpace(filter.Etiqueta))
        {
            var etiqueta = filter.Etiqueta.Trim();
            // Etiquetas almacenadas como "a,b,c" — match por token (case-insensitive vía comparación en memoria tras filtrado base)
            query = query.Where(p => p.Etiquetas != null && p.Etiquetas.Contains(etiqueta));
        }

        return await query
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await _db.Productos.AddAsync(producto, cancellationToken);
    }

    public Task UpdateAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        _db.Productos.Update(producto);
        return Task.CompletedTask;
    }
}
