using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;

    public NotificacionRepository(AppDbContext db) => _db = db;

    public Task<Notificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Notificaciones.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notificacion>> ListByUsuarioAsync(
        int usuarioId,
        bool? leida,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Notificaciones.Where(x => x.UsuarioId == usuarioId);
        if (leida.HasValue)
            query = query.Where(x => x.Leida == leida.Value);

        return await query
            .OrderByDescending(x => x.FechaUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountNoLeidasAsync(int usuarioId, CancellationToken cancellationToken = default)
        => _db.Notificaciones.CountAsync(
            x => x.UsuarioId == usuarioId && !x.Leida,
            cancellationToken);

    public async Task AddAsync(Notificacion notificacion, CancellationToken cancellationToken = default)
    {
        await _db.Notificaciones.AddAsync(notificacion, cancellationToken);
    }
}
