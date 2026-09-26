using PatioElOlvidado.Application.DTOs.Proveedores;

namespace PatioElOlvidado.Application.Interfaces;

public interface IProveedorService
{
    Task<IReadOnlyList<ProveedorDto>> ListAsync(
        ProveedorFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<ProveedorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProveedorDto> CreateAsync(CreateProveedorRequest request, CancellationToken cancellationToken = default);

    Task<ProveedorDto> UpdateAsync(
        int id,
        UpdateProveedorRequest request,
        CancellationToken cancellationToken = default);
}
