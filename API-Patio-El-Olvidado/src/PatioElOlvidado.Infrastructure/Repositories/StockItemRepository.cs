using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly AppDbContext _db;

    public StockItemRepository(AppDbContext db) => _db = db;

    public Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.StockItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StockItem?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => _db.StockItems.FirstOrDefaultAsync(x => x.Nombre == nombre, cancellationToken);

    public async Task<IReadOnlyList<StockItem>> ListAsync(
        bool? activo,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockItems.AsQueryable();
        if (activo.HasValue)
            query = query.Where(x => x.Activo == activo.Value);

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        await _db.StockItems.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        _db.StockItems.Update(item);
        return Task.CompletedTask;
    }
}
