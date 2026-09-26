using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Proveedores;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-11: Admin crea, edita y da de baja lógica. La autorización está en el controller.
/// El nombre es único sin distinguir mayúsculas.
/// </summary>
public class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _proveedores;
    private readonly IUnitOfWork _unitOfWork;

    public ProveedorService(IProveedorRepository proveedores, IUnitOfWork unitOfWork)
    {
        _proveedores = proveedores;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProveedorDto>> ListAsync(
        ProveedorFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _proveedores.ListAsync(filter.Activo, cancellationToken);
        IEnumerable<Proveedor> query = items;

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(x =>
                x.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (x.Contacto?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.Select(Map).ToList();
    }

    public async Task<ProveedorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var proveedor = await _proveedores.GetByIdAsync(id, cancellationToken);
        return proveedor is null ? null : Map(proveedor);
    }

    public async Task<ProveedorDto> CreateAsync(
        CreateProveedorRequest request,
        CancellationToken cancellationToken = default)
    {
        var nombre = RequireNombre(request.Nombre);
        await EnsureNombreUnicoAsync(nombre, null, cancellationToken);

        var proveedor = new Proveedor
        {
            Nombre = nombre,
            Contacto = NormalizeOptional(request.Contacto),
            Telefono = NormalizeOptional(request.Telefono),
            Email = NormalizeOptional(request.Email),
            Notas = NormalizeOptional(request.Notas),
            Activo = request.Activo
        };

        await _proveedores.AddAsync(proveedor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    public async Task<ProveedorDto> UpdateAsync(
        int id,
        UpdateProveedorRequest request,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await _proveedores.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Proveedor no encontrado.", StatusCodes.Status404NotFound);

        var nombre = RequireNombre(request.Nombre);
        await EnsureNombreUnicoAsync(nombre, proveedor.Id, cancellationToken);

        proveedor.Nombre = nombre;
        proveedor.Contacto = NormalizeOptional(request.Contacto);
        proveedor.Telefono = NormalizeOptional(request.Telefono);
        proveedor.Email = NormalizeOptional(request.Email);
        proveedor.Notas = NormalizeOptional(request.Notas);
        proveedor.Activo = request.Activo;

        await _proveedores.UpdateAsync(proveedor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    private async Task EnsureNombreUnicoAsync(
        string nombre,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var items = await _proveedores.ListAsync(null, cancellationToken);
        var duplicado = items.Any(x =>
            x.Id != excludeId
            && string.Equals(x.Nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase));

        if (duplicado)
            throw new AppException("Ya existe un proveedor con ese nombre.", StatusCodes.Status409Conflict);
    }

    private static string RequireNombre(string? nombre)
    {
        var trimmed = nombre?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new AppException("El nombre es obligatorio.", StatusCodes.Status400BadRequest);
        if (trimmed.Length > 100)
            throw new AppException("El nombre admite hasta 100 caracteres.", StatusCodes.Status400BadRequest);
        return trimmed;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProveedorDto Map(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        Nombre = proveedor.Nombre,
        Contacto = proveedor.Contacto,
        Telefono = proveedor.Telefono,
        Email = proveedor.Email,
        Notas = proveedor.Notas,
        Activo = proveedor.Activo
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
