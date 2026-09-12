using PatioElOlvidado.Application.DTOs.Pagos;

namespace PatioElOlvidado.Application.Interfaces;

public interface IPagoService
{
    Task<PagoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PagoDto>> ListByPedidoIdAsync(int pedidoId, CancellationToken cancellationToken = default);
    Task<PagoDto> CreateAsync(CreatePagoRequest request, CancellationToken cancellationToken = default);
    Task<PagoDto> AnularAsync(int id, CancellationToken cancellationToken = default);
}
