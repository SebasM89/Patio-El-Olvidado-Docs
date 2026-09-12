using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        => await _db.RefreshTokens.AddAsync(token, cancellationToken);

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => _db.RefreshTokens
            .Include(t => t.Usuario)
            .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _db.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }
}
