using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> SearchAsync(UsuarioFilterQuery filter, CancellationToken cancellationToken = default);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string emailNormalized, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CountActiveAdminsAsync(int? excludeId = null, CancellationToken cancellationToken = default);
}
