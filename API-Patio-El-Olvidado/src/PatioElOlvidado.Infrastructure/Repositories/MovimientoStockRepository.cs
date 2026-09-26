using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly AppDbContext _db;

    public MovimientoStockRepository(AppDbContext db) => _db = db;

    public Task<MovimientoStock?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.MovimientosStock.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MovimientoStock>> ListByStockItemAsync(
        int stockItemId,
        CancellationToken cancellationToken = default)
        => await _db.MovimientosStock
            .Include(x => x.Proveedor)
            .Where(x => x.StockItemId == stockItemId)
            .OrderByDescending(x => x.FechaUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MovimientoStock movimiento, CancellationToken cancellationToken = default)
    {
        await _db.MovimientosStock.AddAsync(movimiento, cancellationToken);
    }
}
