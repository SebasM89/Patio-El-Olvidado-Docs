using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface ILiquidacionRepository
{
    Task<IReadOnlyList<Liquidacion>> ListByEmpleadoIdAsync(int empleadoId, CancellationToken cancellationToken = default);
    /// <summary>Liquidaciones cuyo periodo intersecta [desde, hasta] (PeriodoDesde ≤ hasta AND PeriodoHasta ≥ desde). Incluye Empleado.</summary>
    Task<IReadOnlyList<Liquidacion>> ListByPeriodoIntersectAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
    Task AddAsync(Liquidacion liquidacion, CancellationToken cancellationToken = default);
}
