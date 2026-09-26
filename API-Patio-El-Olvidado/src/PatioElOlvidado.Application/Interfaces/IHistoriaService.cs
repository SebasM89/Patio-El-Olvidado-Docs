using PatioElOlvidado.Application.DTOs.Historia;

namespace PatioElOlvidado.Application.Interfaces;

public interface IHistoriaService
{
    Task<HistoriaDto?> GetAsync(string rol, CancellationToken cancellationToken = default);

    Task<HistoriaDto> UpdateAsync(
        UpdateHistoriaRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default);
}
