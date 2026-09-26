using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class HistoriaRepository : IHistoriaRepository
{
    private readonly AppDbContext _db;

    public HistoriaRepository(AppDbContext db) => _db = db;

    public Task<HistoriaRestaurante?> GetAsync(CancellationToken cancellationToken = default)
        => _db.HistoriaRestaurante.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

    public Task UpdateAsync(HistoriaRestaurante historia, CancellationToken cancellationToken = default)
    {
        _db.HistoriaRestaurante.Update(historia);
        return Task.CompletedTask;
    }
}
