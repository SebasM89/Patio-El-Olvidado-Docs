using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class MesaRepository : IMesaRepository
{
    private readonly AppDbContext _db;

    public MesaRepository(AppDbContext db) => _db = db;

    public Task<Mesa?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Mesas.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Mesa?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default)
        => _db.Mesas.FirstOrDefaultAsync(m => m.Numero == numero, cancellationToken);

    public async Task<IReadOnlyList<Mesa>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Mesas.OrderBy(m => m.Numero).ToListAsync(cancellationToken);
}
