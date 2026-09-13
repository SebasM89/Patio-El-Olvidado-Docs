using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly AppDbContext _db;

    public PagoRepository(AppDbContext db) => _db = db;

    public Task<Pago?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Pagos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pago>> ListByPedidoIdAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Pagos
            .Where(p => p.PedidoId == pedidoId)
            .OrderBy(p => p.FechaPago)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> SumCompletadosByPedidoIdAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Pagos
            .Where(p => p.PedidoId == pedidoId && p.Estado == PagoEstado.Completado)
            .SumAsync(p => (decimal?)p.Monto, cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyList<Pago>> ListCompletadosByFechaPagoRangoAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _db.Pagos
            .Where(p =>
                p.Estado == PagoEstado.Completado
                && p.FechaPago >= fromUtc
                && p.FechaPago < toExclusiveUtc)
            .OrderBy(p => p.FechaPago)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Pago pago, CancellationToken cancellationToken = default)
    {
        await _db.Pagos.AddAsync(pago, cancellationToken);
    }

    public Task UpdateAsync(Pago pago, CancellationToken cancellationToken = default)
    {
        _db.Pagos.Update(pago);
        return Task.CompletedTask;
    }
}
