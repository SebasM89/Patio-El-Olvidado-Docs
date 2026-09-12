using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Clientes;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;

    public ClienteRepository(AppDbContext db) => _db = db;

    public Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> GetByUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default)
        => _db.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlyList<Cliente>> SearchAsync(
        ClienteFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Clientes.AsQueryable();

        if (filter.Activo.HasValue)
            query = query.Where(c => c.Activo == filter.Activo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(c =>
                c.Nombre.Contains(q) ||
                c.Telefono.Contains(q) ||
                (c.Email != null && c.Email.Contains(q)));
        }

        return await query
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _db.Clientes.AddAsync(cliente, cancellationToken);
    }

    public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _db.Clientes.Update(cliente);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Clientes.Where(c => c.Email == email);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> UsuarioIdExistsAsync(
        int usuarioId,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Clientes.Where(c => c.UsuarioId == usuarioId);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }
}
