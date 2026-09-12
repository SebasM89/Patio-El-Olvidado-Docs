using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Empleados;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly AppDbContext _db;

    public EmpleadoRepository(AppDbContext db) => _db = db;

    public Task<Empleado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Empleados.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Empleado?> GetByUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default)
        => _db.Empleados.FirstOrDefaultAsync(e => e.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlyList<Empleado>> SearchAsync(
        EmpleadoFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Empleados.AsQueryable();

        if (filter.Activo.HasValue)
            query = query.Where(e => e.Activo == filter.Activo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(e =>
                e.Nombre.Contains(q) ||
                (e.Puesto != null && e.Puesto.Contains(q)) ||
                (e.Telefono != null && e.Telefono.Contains(q)));
        }

        return await query
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        await _db.Empleados.AddAsync(empleado, cancellationToken);
    }

    public Task UpdateAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        _db.Empleados.Update(empleado);
        return Task.CompletedTask;
    }

    public Task<bool> UsuarioIdExistsAsync(
        int usuarioId,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Empleados.Where(e => e.UsuarioId == usuarioId);
        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }
}
