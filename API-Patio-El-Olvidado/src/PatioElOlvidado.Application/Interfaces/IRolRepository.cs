using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IRolRepository
{
    Task<Rol?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rol>> ListAsync(CancellationToken cancellationToken = default);
}
