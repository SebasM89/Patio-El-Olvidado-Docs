using PatioElOlvidado.Application.DTOs.Mesas;

namespace PatioElOlvidado.Application.Interfaces;

public interface IMesaService
{
    Task<IReadOnlyList<MesaDto>> ListAsync(CancellationToken cancellationToken = default);
}
