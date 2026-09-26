using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
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

    public async Task<IReadOnlyList<Usuario>> SearchAsync(
        UsuarioFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Usuarios.Include(u => u.Rol).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim().ToLower();
            query = query.Where(u =>
                u.Nombre.ToLower().Contains(q) ||
                u.Email.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(filter.Rol))
        {
            var rol = filter.Rol.Trim().ToLower();
            query = query.Where(u => u.Rol.Nombre.ToLower() == rol);
        }

        if (!string.IsNullOrWhiteSpace(filter.Estado))
        {
            var estado = filter.Estado.Trim().ToLower();
            query = query.Where(u => u.Estado.ToLower() == estado);
        }

        return await query
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => await _db.Usuarios.AddAsync(usuario, cancellationToken);

    public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _db.Usuarios.Update(usuario);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(
        string emailNormalized,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Usuarios.Where(u => u.Email.ToLower() == emailNormalized);
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Usuarios.Where(u =>
            u.Rol.Nombre == RolesSistema.Admin &&
            u.Estado == UsuarioEstado.Activo);

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return query.CountAsync(cancellationToken);
    }
}
