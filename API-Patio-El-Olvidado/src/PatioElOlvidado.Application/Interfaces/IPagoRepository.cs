using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IPagoRepository
{
    Task<Pago?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pago>> ListByPedidoIdAsync(int pedidoId, CancellationToken cancellationToken = default);
    Task<decimal> SumCompletadosByPedidoIdAsync(int pedidoId, CancellationToken cancellationToken = default);
    Task AddAsync(Pago pago, CancellationToken cancellationToken = default);
    Task UpdateAsync(Pago pago, CancellationToken cancellationToken = default);
}
