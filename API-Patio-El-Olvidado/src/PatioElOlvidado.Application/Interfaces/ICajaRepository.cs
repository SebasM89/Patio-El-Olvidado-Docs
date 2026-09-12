using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface ICajaRepository
{
    Task<Caja?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Caja?> GetByFechaAsync(DateOnly fecha, CancellationToken cancellationToken = default);
    Task AddAsync(Caja caja, CancellationToken cancellationToken = default);
    Task UpdateAsync(Caja caja, CancellationToken cancellationToken = default);
}
