using PatioElOlvidado.Application.DTOs.Usuarios;

namespace PatioElOlvidado.Application.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListAsync(UsuarioFilterQuery filter, CancellationToken cancellationToken = default);
    Task<UsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolCatalogoDto>> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<UsuarioDto> CreateAsync(CreateUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioDto> UpdateAsync(int id, UpdateUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoUsuarioRequest request,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);
}
