using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>Lectura del catálogo Mesas. Sin altas, bajas ni modificaciones.</summary>
public interface IMesaRepository
{
    Task<Mesa?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Mesa?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mesa>> ListAsync(CancellationToken cancellationToken = default);
}
