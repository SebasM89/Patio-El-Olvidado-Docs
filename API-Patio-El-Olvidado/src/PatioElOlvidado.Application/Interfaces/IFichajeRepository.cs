using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IFichajeRepository
{
    Task<Fichaje?> GetAbiertoByEmpleadoIdAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fichaje>> ListByEmpleadoIdAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fichaje>> ListCerradosEnPeriodoAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
    Task AddAsync(Fichaje fichaje, CancellationToken cancellationToken = default);
    Task UpdateAsync(Fichaje fichaje, CancellationToken cancellationToken = default);
}
