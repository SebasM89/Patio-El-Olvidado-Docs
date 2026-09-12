using PatioElOlvidado.Application.DTOs.Empleados;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IEmpleadoRepository
{
    Task<Empleado?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Empleado?> GetByUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Empleado>> SearchAsync(EmpleadoFilterQuery filter, CancellationToken cancellationToken = default);
    Task AddAsync(Empleado empleado, CancellationToken cancellationToken = default);
    Task UpdateAsync(Empleado empleado, CancellationToken cancellationToken = default);
    Task<bool> UsuarioIdExistsAsync(int usuarioId, int? excludeId = null, CancellationToken cancellationToken = default);
}
