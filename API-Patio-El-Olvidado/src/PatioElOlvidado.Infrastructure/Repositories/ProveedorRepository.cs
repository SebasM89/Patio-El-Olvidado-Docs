using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _db;

    public ProveedorRepository(AppDbContext db) => _db = db;

    public Task<Proveedor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Proveedores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Proveedor>> ListAsync(
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Proveedores.AsQueryable();
        if (activo.HasValue)
            query = query.Where(x => x.Activo == activo.Value);

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
    {
        await _db.Proveedores.AddAsync(proveedor, cancellationToken);
    }

    public Task UpdateAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
    {
        _db.Proveedores.Update(proveedor);
        return Task.CompletedTask;
    }
}
