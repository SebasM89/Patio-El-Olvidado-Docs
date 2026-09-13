using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class LiquidacionRepository : ILiquidacionRepository
{
    private readonly AppDbContext _db;

    public LiquidacionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Liquidacion>> ListByEmpleadoIdAsync(
        int empleadoId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Liquidaciones
            .Where(l => l.EmpleadoId == empleadoId)
            .OrderByDescending(l => l.GeneradaEnUtc)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Liquidacion>> ListByPeriodoIntersectAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        return await _db.Liquidaciones
            .Include(l => l.Empleado)
            .Where(l => l.PeriodoDesde <= hasta && l.PeriodoHasta >= desde)
            .OrderBy(l => l.PeriodoDesde)
            .ThenBy(l => l.EmpleadoId)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Liquidacion liquidacion, CancellationToken cancellationToken = default)
    {
        await _db.Liquidaciones.AddAsync(liquidacion, cancellationToken);
    }
}
