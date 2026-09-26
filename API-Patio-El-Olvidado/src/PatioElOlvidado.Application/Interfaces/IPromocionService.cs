using PatioElOlvidado.Application.DTOs.Promociones;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>
/// RN-13. El rol llega del claim; no se filtra por usuarioId.
/// Una promoción no escribe pedidos ni visitas.
/// </summary>
public interface IPromocionService
{
    Task<IReadOnlyList<PromocionDto>> ListAsync(
        PromocionFilterQuery filter,
        string rol,
        CancellationToken cancellationToken = default);

    Task<PromocionDto?> GetByIdAsync(int id, string rol, CancellationToken cancellationToken = default);

    Task<PromocionDto> CreateAsync(
        CreatePromocionRequest request,
        string rol,
        CancellationToken cancellationToken = default);

    Task<PromocionDto> UpdateAsync(
        int id,
        UpdatePromocionRequest request,
        string rol,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string rol, CancellationToken cancellationToken = default);
}
