using PatioElOlvidado.Application.DTOs.Clientes;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cliente>> SearchAsync(ClienteFilterQuery filter, CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> UsuarioIdExistsAsync(int usuarioId, int? excludeId = null, CancellationToken cancellationToken = default);
}
