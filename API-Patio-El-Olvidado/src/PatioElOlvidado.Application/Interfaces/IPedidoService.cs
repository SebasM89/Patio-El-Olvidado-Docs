using PatioElOlvidado.Application.DTOs.Pedidos;

namespace PatioElOlvidado.Application.Interfaces;

public interface IPedidoService
{
    Task<PedidoDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PedidoDto>> ListAsync(
        PedidoFilterQuery filter,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<PedidoDto> CreateAsync(
        CreatePedidoRequest request,
        int creadoPorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<PedidoDto> UpdateAsync(
        int id,
        UpdatePedidoRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<PedidoDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoPedidoRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);
}
