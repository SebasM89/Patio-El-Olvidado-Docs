using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Promociones;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-13: catálogo de avisos con vigencia. No escribe Pedido.Subtotal, Pedido.Total ni Clientes.Visitas.
/// No se combina con RN-05. El alcance de cada operación sale del claim de rol, no de un usuarioId.
/// </summary>
public class PromocionService : IPromocionService
{
    private readonly IPromocionRepository _promociones;
    private readonly IUnitOfWork _unitOfWork;

    public PromocionService(IPromocionRepository promociones, IUnitOfWork unitOfWork)
    {
        _promociones = promociones;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PromocionDto>> ListAsync(
        PromocionFilterQuery filter,
        string rol,
        CancellationToken cancellationToken = default)
    {
        EnsureConsulta(rol);

        if (IsCliente(rol))
        {
            var visibles = await _promociones.ListAsync(null, true, true, cancellationToken);
            return visibles.Select(Map).ToList();
        }

        filter ??= new PromocionFilterQuery();
        // El repositorio acopla vigente=true con Activo. En el admin son filtros distintos:
        // vigente = las fechas cubren hoy (UTC, inclusive), sin exigir Activo.
        var items = await _promociones.ListAsync(filter.Q, filter.Activo, null, cancellationToken);
        if (filter.Vigente.HasValue)
            items = FiltrarCoberturaHoy(items, filter.Vigente.Value);

        return items.Select(Map).ToList();
    }

    public async Task<PromocionDto?> GetByIdAsync(int id, string rol, CancellationToken cancellationToken = default)
    {
        EnsureConsulta(rol);

        var promocion = await _promociones.GetByIdAsync(id, cancellationToken);
        if (promocion is null)
            return null;

        if (IsCliente(rol) && !VisibleParaCliente(promocion))
            return null;

        return Map(promocion);
    }

    public async Task<PromocionDto> CreateAsync(
        CreatePromocionRequest request,
        string rol,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(rol);
        var nombre = RequireNombre(request.Nombre);
        var descripcion = RequireDescripcion(request.Descripcion);
        EnsureVigencia(request.VigenteDesde, request.VigenteHasta);

        var promocion = new Promocion
        {
            Nombre = nombre,
            Descripcion = descripcion,
            VigenteDesde = request.VigenteDesde,
            VigenteHasta = request.VigenteHasta,
            Activo = true
        };

        await _promociones.AddAsync(promocion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(promocion);
    }

    public async Task<PromocionDto> UpdateAsync(
        int id,
        UpdatePromocionRequest request,
        string rol,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(rol);

        var promocion = await _promociones.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Promoción no encontrada.", StatusCodes.Status404NotFound);

        promocion.Nombre = RequireNombre(request.Nombre);
        promocion.Descripcion = RequireDescripcion(request.Descripcion);
        EnsureVigencia(request.VigenteDesde, request.VigenteHasta);
        promocion.VigenteDesde = request.VigenteDesde;
        promocion.VigenteHasta = request.VigenteHasta;
        promocion.Activo = request.Activo;

        await _promociones.UpdateAsync(promocion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(promocion);
    }

    public async Task DeleteAsync(int id, string rol, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(rol);

        var promocion = await _promociones.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Promoción no encontrada.", StatusCodes.Status404NotFound);

        if (!promocion.Activo)
            return;

        promocion.Activo = false;
        await _promociones.UpdateAsync(promocion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Promocion> FiltrarCoberturaHoy(IReadOnlyList<Promocion> items, bool vigente)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return items.Where(x =>
        {
            var cubreHoy = x.VigenteDesde <= hoy && x.VigenteHasta >= hoy;
            return vigente ? cubreHoy : !cubreHoy;
        }).ToList();
    }

    private static bool VisibleParaCliente(Promocion promocion)
    {
        if (!promocion.Activo)
            return false;

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return promocion.VigenteDesde <= hoy && promocion.VigenteHasta >= hoy;
    }

    private static void EnsureConsulta(string rol)
    {
        if (!IsAdmin(rol) && !IsCliente(rol))
            throw new AppException("No tenés permiso para consultar promociones.", StatusCodes.Status403Forbidden);
    }

    private static void EnsureAdmin(string rol)
    {
        if (!IsAdmin(rol))
            throw new AppException("Solo el administrador puede gestionar promociones.", StatusCodes.Status403Forbidden);
    }

    private static bool IsAdmin(string rol)
        => string.Equals(rol, RolesSistema.Admin, StringComparison.Ordinal);

    private static bool IsCliente(string rol)
        => string.Equals(rol, RolesSistema.Cliente, StringComparison.Ordinal);

    private static string RequireNombre(string? nombre)
    {
        var trimmed = nombre?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new AppException("El nombre es obligatorio.", StatusCodes.Status400BadRequest);
        if (trimmed.Length > 100)
            throw new AppException("El nombre admite hasta 100 caracteres.", StatusCodes.Status400BadRequest);
        return trimmed;
    }

    private static string RequireDescripcion(string? descripcion)
    {
        var trimmed = descripcion?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new AppException("La descripción es obligatoria.", StatusCodes.Status400BadRequest);
        if (trimmed.Length > 500)
            throw new AppException("La descripción admite hasta 500 caracteres.", StatusCodes.Status400BadRequest);
        return trimmed;
    }

    private static void EnsureVigencia(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            throw new AppException(
                "La vigencia hasta no puede ser anterior a la vigencia desde.",
                StatusCodes.Status400BadRequest);
    }

    private static PromocionDto Map(Promocion promocion) => new()
    {
        Id = promocion.Id,
        Nombre = promocion.Nombre,
        Descripcion = promocion.Descripcion,
        VigenteDesde = promocion.VigenteDesde,
        VigenteHasta = promocion.VigenteHasta,
        Activo = promocion.Activo
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
    }
}
