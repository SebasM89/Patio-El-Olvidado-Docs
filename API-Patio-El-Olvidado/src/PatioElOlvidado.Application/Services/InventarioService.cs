using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Inventario;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-09: CantidadActual solo cambia junto con un movimiento, en la misma transacción.
/// RN-10: la autorización (Admin muta, Empleado consulta) está en el controller.
/// </summary>
public class InventarioService : IInventarioService
{
    public const string MotivoAltaInicial = "Alta inicial";

    private readonly IStockItemRepository _items;
    private readonly IMovimientoStockRepository _movimientos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;

    public InventarioService(
        IStockItemRepository items,
        IMovimientoStockRepository movimientos,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _movimientos = movimientos;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<StockItemDto>> ListAsync(
        StockItemFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _items.ListAsync(filter.Activo, cancellationToken);
        IEnumerable<StockItem> query = items;

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(x =>
                x.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (x.Descripcion?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (filter.EnAlerta.HasValue)
            query = query.Where(x => EstaEnAlerta(x) == filter.EnAlerta.Value);

        return query.Select(MapItem).ToList();
    }

    public Task<IReadOnlyList<StockItemDto>> ListAlertasAsync(CancellationToken cancellationToken = default)
        => ListAsync(new StockItemFilterQuery { EnAlerta = true }, cancellationToken);

    public async Task<StockItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken);
        return item is null ? null : MapItem(item);
    }

    public Task<StockItemDto> CreateAsync(
        CreateStockItemRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.ExecuteInTransactionAsync(
            ct => CreateCoreAsync(request, registradoPorUsuarioId, ct),
            cancellationToken);

    public async Task<StockItemDto> UpdateAsync(
        int id,
        UpdateStockItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Ítem de stock no encontrado.", StatusCodes.Status404NotFound);

        var nombre = RequireNombre(request.Nombre);
        await EnsureNombreUnicoAsync(nombre, item.Id, cancellationToken);

        if (request.StockMinimo < 0)
            throw new AppException("El stock mínimo no puede ser negativo.", StatusCodes.Status400BadRequest);

        var cantidad = item.CantidadActual;
        var unidad = item.Unidad;

        item.Nombre = nombre;
        item.Descripcion = NormalizeOptional(request.Descripcion);
        item.StockMinimo = request.StockMinimo;
        item.Activo = request.Activo;
        item.CantidadActual = cantidad;
        item.Unidad = unidad;

        await _items.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task<IReadOnlyList<MovimientoStockDto>> ListMovimientosAsync(
        int stockItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _items.GetByIdAsync(stockItemId, cancellationToken)
            ?? throw new AppException("Ítem de stock no encontrado.", StatusCodes.Status404NotFound);

        var movimientos = await _movimientos.ListByStockItemAsync(item.Id, cancellationToken);
        var nombres = await NombresUsuariosAsync(
            movimientos.Select(m => m.RegistradoPorUsuarioId),
            cancellationToken);

        return movimientos
            .Select(m => MapMovimiento(
                m,
                nombres.TryGetValue(m.RegistradoPorUsuarioId, out var nombre) ? nombre : null))
            .ToList();
    }

    public Task<MovimientoStockDto> RegistrarMovimientoAsync(
        int stockItemId,
        RegistrarMovimientoRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.ExecuteInTransactionAsync(
            ct => RegistrarCoreAsync(stockItemId, request, registradoPorUsuarioId, ct),
            cancellationToken);

    private async Task<StockItemDto> CreateCoreAsync(
        CreateStockItemRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken)
    {
        var nombre = RequireNombre(request.Nombre);
        if (!InventarioEnumParser.TryParseUnidad(request.Unidad, out var unidad))
            throw new AppException("La unidad debe ser Unidad, Kg o L.", StatusCodes.Status400BadRequest);

        if (request.StockMinimo < 0)
            throw new AppException("El stock mínimo no puede ser negativo.", StatusCodes.Status400BadRequest);

        if (request.CantidadInicial < 0)
            throw new AppException("La cantidad inicial no puede ser negativa.", StatusCodes.Status400BadRequest);

        if (request.CantidadInicial > 0 && !request.Activo)
            throw new AppException("Un ítem inactivo no acepta movimientos.", StatusCodes.Status400BadRequest);

        await EnsureNombreUnicoAsync(nombre, null, cancellationToken);

        var item = new StockItem
        {
            Nombre = nombre,
            Descripcion = NormalizeOptional(request.Descripcion),
            Unidad = unidad,
            CantidadActual = 0,
            StockMinimo = request.StockMinimo,
            Activo = request.Activo
        };

        await _items.AddAsync(item, cancellationToken);

        if (request.CantidadInicial > 0)
        {
            var usuario = await RequireUsuarioAsync(registradoPorUsuarioId, cancellationToken);
            item.CantidadActual = request.CantidadInicial;
            await _movimientos.AddAsync(new MovimientoStock
            {
                StockItem = item,
                Tipo = TipoMovimientoStock.Entrada,
                Cantidad = request.CantidadInicial,
                Motivo = MotivoAltaInicial,
                FechaUtc = DateTime.UtcNow,
                RegistradoPorUsuarioId = usuario.Id,
                RegistradoPorUsuario = usuario
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    private async Task<MovimientoStockDto> RegistrarCoreAsync(
        int stockItemId,
        RegistrarMovimientoRequest request,
        int registradoPorUsuarioId,
        CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(stockItemId, cancellationToken)
            ?? throw new AppException("Ítem de stock no encontrado.", StatusCodes.Status404NotFound);

        if (!item.Activo)
            throw new AppException("Un ítem inactivo no acepta movimientos.", StatusCodes.Status400BadRequest);

        if (!InventarioEnumParser.TryParseTipo(request.Tipo, out var tipo))
            throw new AppException("El tipo debe ser Entrada o Salida.", StatusCodes.Status400BadRequest);

        if (request.Cantidad <= 0)
            throw new AppException("La cantidad debe ser mayor a cero.", StatusCodes.Status400BadRequest);

        if (tipo == TipoMovimientoStock.Salida && request.Cantidad > item.CantidadActual)
            throw new AppException("La salida supera el saldo disponible.", StatusCodes.Status400BadRequest);

        var usuario = await RequireUsuarioAsync(registradoPorUsuarioId, cancellationToken);

        var nuevoSaldo = tipo == TipoMovimientoStock.Entrada
            ? item.CantidadActual + request.Cantidad
            : item.CantidadActual - request.Cantidad;

        if (nuevoSaldo < 0)
            throw new AppException("La salida supera el saldo disponible.", StatusCodes.Status400BadRequest);

        var movimiento = new MovimientoStock
        {
            StockItem = item,
            StockItemId = item.Id,
            Tipo = tipo,
            Cantidad = request.Cantidad,
            Motivo = NormalizeOptional(request.Motivo),
            FechaUtc = DateTime.UtcNow,
            RegistradoPorUsuarioId = usuario.Id,
            RegistradoPorUsuario = usuario
        };

        item.CantidadActual = nuevoSaldo;
        await _movimientos.AddAsync(movimiento, cancellationToken);
        await _items.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapMovimiento(movimiento, usuario.Nombre);
    }

    private async Task EnsureNombreUnicoAsync(
        string nombre,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var items = await _items.ListAsync(null, cancellationToken);
        var duplicado = items.Any(x =>
            x.Id != excludeId
            && string.Equals(x.Nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase));

        if (duplicado)
            throw new AppException("Ya existe un ítem con ese nombre.", StatusCodes.Status409Conflict);
    }

    private async Task<Usuario> RequireUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
        => await _usuarios.GetByIdAsync(usuarioId, cancellationToken)
           ?? throw new AppException("Usuario no encontrado.", StatusCodes.Status400BadRequest);

    private async Task<Dictionary<int, string>> NombresUsuariosAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, string>();
        foreach (var id in ids.Distinct())
        {
            var usuario = await _usuarios.GetByIdAsync(id, cancellationToken);
            if (usuario is not null)
                map[id] = usuario.Nombre;
        }

        return map;
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

    internal static bool EstaEnAlerta(StockItem item)
        => item.Activo && item.CantidadActual <= item.StockMinimo;

    private static StockItemDto MapItem(StockItem item) => new()
    {
        Id = item.Id,
        Nombre = item.Nombre,
        Descripcion = item.Descripcion,
        Unidad = item.Unidad.ToString(),
        CantidadActual = item.CantidadActual,
        StockMinimo = item.StockMinimo,
        Activo = item.Activo,
        EnAlerta = EstaEnAlerta(item)
    };

    private static MovimientoStockDto MapMovimiento(MovimientoStock movimiento, string? registradoPorNombre) => new()
    {
        Id = movimiento.Id,
        StockItemId = movimiento.StockItemId,
        Tipo = movimiento.Tipo.ToString(),
        Cantidad = movimiento.Cantidad,
        Motivo = movimiento.Motivo,
        FechaUtc = movimiento.FechaUtc,
        RegistradoPorUsuarioId = movimiento.RegistradoPorUsuarioId,
        RegistradoPorNombre = registradoPorNombre
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
