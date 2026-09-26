using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class PromocionRepository : IPromocionRepository
{
    private readonly AppDbContext _db;

    public PromocionRepository(AppDbContext db) => _db = db;

    public Task<Promocion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Promociones.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Promocion>> ListAsync(
        string? q,
        bool? activo,
        bool? vigente,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Promociones.AsQueryable();

        if (activo.HasValue)
            query = query.Where(x => x.Activo == activo.Value);

        if (vigente.HasValue)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            if (vigente.Value)
            {
                query = query.Where(x =>
                    x.Activo &&
                    x.VigenteDesde <= hoy &&
                    x.VigenteHasta >= hoy);
            }
            else
            {
                query = query.Where(x =>
                    !x.Activo ||
                    x.VigenteDesde > hoy ||
                    x.VigenteHasta < hoy);
            }
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Nombre.Contains(term) ||
                x.Descripcion.Contains(term));
        }

        return await query
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Promocion promocion, CancellationToken cancellationToken = default)
    {
        await _db.Promociones.AddAsync(promocion, cancellationToken);
    }

    public Task UpdateAsync(Promocion promocion, CancellationToken cancellationToken = default)
    {
        _db.Promociones.Update(promocion);
        return Task.CompletedTask;
    }
}
