using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _db.Usuarios.Update(usuario);
        return Task.CompletedTask;
    }
}
