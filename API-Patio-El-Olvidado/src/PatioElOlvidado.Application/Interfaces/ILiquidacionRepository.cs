using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface ILiquidacionRepository
{
    Task<IReadOnlyList<Liquidacion>> ListByEmpleadoIdAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task AddAsync(Liquidacion liquidacion, CancellationToken cancellationToken = default);
}
