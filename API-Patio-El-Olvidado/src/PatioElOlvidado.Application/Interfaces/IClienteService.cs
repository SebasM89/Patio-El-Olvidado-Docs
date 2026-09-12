using PatioElOlvidado.Application.DTOs.Clientes;

namespace PatioElOlvidado.Application.Interfaces;

public interface IClienteService
{
    Task<ClienteDto?> GetByIdAsync(int id, int usuarioId, string rol, CancellationToken cancellationToken = default);
    Task<ClienteDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClienteDto>> ListAsync(ClienteFilterQuery filter, CancellationToken cancellationToken = default);
    Task<ClienteDto> CreateAsync(CreateClienteRequest request, CancellationToken cancellationToken = default);
    Task<ClienteDto> UpdateAsync(int id, UpdateClienteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialConsumoItemDto>> GetHistorialAsync(
        int clienteId,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialConsumoItemDto>> GetMeHistorialAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
