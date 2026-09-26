using PatioElOlvidado.Application.DTOs.Reservas;

namespace PatioElOlvidado.Application.Interfaces;

public interface IReservaService
{
    Task<ReservaDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservaDto>> ListAsync(
        ReservaFilterQuery filter,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<ReservaDto> CreateAsync(
        CreateReservaRequest request,
        int creadoPorUsuarioId,
        string rol,
        CancellationToken cancellationToken = default);

    Task<ReservaDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoReservaRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);
}
