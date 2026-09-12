using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pedido>> SearchAsync(
        PedidoFilterQuery filter,
        int? soloCreadoPorUsuarioId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default);
    Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default);
    void RemoveDetalles(IEnumerable<DetallePedido> detalles);
}
