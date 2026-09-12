using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class FichajeRepository : IFichajeRepository
{
    private readonly AppDbContext _db;

    public FichajeRepository(AppDbContext db) => _db = db;

    public Task<Fichaje?> GetAbiertoByEmpleadoIdAsync(int empleadoId, CancellationToken cancellationToken = default)
        => _db.Fichajes.FirstOrDefaultAsync(
            f => f.EmpleadoId == empleadoId && f.SalidaUtc == null,
            cancellationToken);

    public async Task<IReadOnlyList<Fichaje>> ListByEmpleadoIdAsync(
        int empleadoId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Fichajes
            .Where(f => f.EmpleadoId == empleadoId)
            .OrderByDescending(f => f.EntradaUtc)
            .ThenByDescending(f => f.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Fichaje>> ListCerradosEnPeriodoAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        // Inclusive date range on EntradaUtc (UTC calendar day).
        var desdeUtc = desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hastaExclusive = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _db.Fichajes
            .Where(f =>
                f.EmpleadoId == empleadoId &&
                f.SalidaUtc != null &&
                f.EntradaUtc >= desdeUtc &&
                f.EntradaUtc < hastaExclusive)
            .OrderBy(f => f.EntradaUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Fichaje fichaje, CancellationToken cancellationToken = default)
    {
        await _db.Fichajes.AddAsync(fichaje, cancellationToken);
    }

    public Task UpdateAsync(Fichaje fichaje, CancellationToken cancellationToken = default)
    {
        _db.Fichajes.Update(fichaje);
        return Task.CompletedTask;
    }
}
