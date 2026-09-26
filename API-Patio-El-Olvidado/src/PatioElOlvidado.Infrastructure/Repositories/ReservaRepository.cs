using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class ReservaRepository : IReservaRepository
{
    private readonly AppDbContext _db;

    public ReservaRepository(AppDbContext db) => _db = db;

    public Task<Reserva?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.Mesa)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reserva>> ListByMesaYFechaAsync(
        int mesaId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
        => await _db.Reservas
            .Where(r => r.MesaId == mesaId && r.Fecha == fecha)
            .OrderBy(r => r.HoraInicio)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reserva>> ListByFechaAsync(
        DateOnly fecha,
        int? mesaId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.Mesa)
            .Where(r => r.Fecha == fecha);

        if (mesaId.HasValue)
            query = query.Where(r => r.MesaId == mesaId.Value);

        return await query
            .OrderBy(r => r.HoraInicio)
            .ThenBy(r => r.Mesa.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reserva>> ListByClienteIdAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
        => await _db.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.Mesa)
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.Fecha)
            .ThenBy(r => r.HoraInicio)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default)
    {
        await _db.Reservas.AddAsync(reserva, cancellationToken);
    }

    public Task UpdateAsync(Reserva reserva, CancellationToken cancellationToken = default)
    {
        _db.Reservas.Update(reserva);
        return Task.CompletedTask;
    }
}
