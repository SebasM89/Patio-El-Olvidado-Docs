using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class CajaRepository : ICajaRepository
{
    private readonly AppDbContext _db;

    public CajaRepository(AppDbContext db) => _db = db;

    public Task<Caja?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Cajas.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Caja?> GetByFechaAsync(DateOnly fecha, CancellationToken cancellationToken = default)
        => _db.Cajas.FirstOrDefaultAsync(c => c.Fecha == fecha, cancellationToken);

    public async Task<IReadOnlyList<Caja>> ListByFechaRangoAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        return await _db.Cajas
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta)
            .OrderBy(c => c.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Caja caja, CancellationToken cancellationToken = default)
    {
        await _db.Cajas.AddAsync(caja, cancellationToken);
    }

    public Task UpdateAsync(Caja caja, CancellationToken cancellationToken = default)
    {
        _db.Cajas.Update(caja);
        return Task.CompletedTask;
    }
}
