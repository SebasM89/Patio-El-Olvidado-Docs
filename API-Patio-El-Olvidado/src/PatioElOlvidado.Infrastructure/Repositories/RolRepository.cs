using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class RolRepository : IRolRepository
{
    private readonly AppDbContext _db;

    public RolRepository(AppDbContext db) => _db = db;

    public Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Rol>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Roles
            .OrderBy(r => r.Nombre)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);
}
