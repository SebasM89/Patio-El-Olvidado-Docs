using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Productos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _productos;
    private readonly IUnitOfWork _unitOfWork;

    public ProductoService(IProductoRepository productos, IUnitOfWork unitOfWork)
    {
        _productos = productos;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByIdAsync(id, cancellationToken);
        return producto is null ? null : Map(producto);
    }

    public async Task<IReadOnlyList<ProductoDto>> ListAsync(
        ProductoFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _productos.SearchAsync(filter, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ProductoDto> CreateAsync(
        CreateProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = NormalizeOptional(request.Descripcion),
            Precio = request.Precio,
            Categoria = request.Categoria.Trim(),
            Imagen = NormalizeOptional(request.Imagen),
            Etiquetas = NormalizeEtiquetas(request.Etiquetas),
            Activo = request.Activo
        };

        await _productos.AddAsync(producto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(producto);
    }

    public async Task<ProductoDto> UpdateAsync(
        int id,
        UpdateProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Producto no encontrado.", StatusCodes.Status404NotFound);

        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = NormalizeOptional(request.Descripcion);
        producto.Precio = request.Precio;
        producto.Categoria = request.Categoria.Trim();
        producto.Imagen = NormalizeOptional(request.Imagen);
        producto.Etiquetas = NormalizeEtiquetas(request.Etiquetas);
        producto.Activo = request.Activo;

        await _productos.UpdateAsync(producto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(producto);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Producto no encontrado.", StatusCodes.Status404NotFound);

        if (!producto.Activo)
            return;

        producto.Activo = false;
        await _productos.UpdateAsync(producto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static ProductoDto Map(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Descripcion = producto.Descripcion,
        Precio = producto.Precio,
        Categoria = producto.Categoria,
        Imagen = producto.Imagen,
        Etiquetas = producto.Etiquetas,
        Activo = producto.Activo
    };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEtiquetas(string? etiquetas)
    {
        if (string.IsNullOrWhiteSpace(etiquetas))
            return null;

        var parts = etiquetas
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var joined = string.Join(',', parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    /// <summary>Evita dependencia de ASP.NET en Application; códigos HTTP estándar.</summary>
    private static class StatusCodes
    {
        public const int Status404NotFound = 404;
    }
}
