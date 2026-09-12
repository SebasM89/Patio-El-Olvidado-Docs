using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        => await _db.PasswordResetTokens.AddAsync(token, cancellationToken);

    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => _db.PasswordResetTokens
            .Include(t => t.Usuario)
            .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _db.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task InvalidateActiveTokensAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var active = await _db.PasswordResetTokens
            .Where(t => t.UsuarioId == usuarioId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.UsedAt = DateTime.UtcNow;
    }
}
