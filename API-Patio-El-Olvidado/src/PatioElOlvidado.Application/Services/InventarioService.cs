using System.Globalization;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Inventario;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-09: CantidadActual solo cambia junto con un movimiento, en la misma transacción.
/// RN-10: la autorización (Admin muta, Empleado consulta) está en el controller.
/// RN-11: proveedor opcional solo en Entrada; debe existir y estar activo. El vínculo no se edita.
/// RN-12: el cruce a alerta inserta un aviso por admin activo, en la misma transacción, antes de guardar.
/// </summary>
public class InventarioService : IInventarioService
{
    public const string MotivoAltaInicial = "Alta inicial";
    public const string TituloStockAlerta = "Stock en alerta";

    private readonly IStockItemRepository _items;
    private readonly IMovimientoStockRepository _movimientos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IProveedorRepository _proveedores;
    private readonly INotificacionRepository _notificaciones;
    private readonly IUnitOfWork _unitOfWork;

    public InventarioService(
        IStockItemRepository items,
        IMovimientoStockRepository movimientos,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork,
        IProveedorRepository proveedores,
        INotificacionRepository notificaciones)
    {
        _items = items;
        _movimientos = movimientos;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
        _proveedores = proveedores;
        _notificaciones = notificaciones;
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

        var estabaEnAlerta = EstaEnAlerta(item);

        if (!InventarioEnumParser.TryParseTipo(request.Tipo, out var tipo))
            throw new AppException("El tipo debe ser Entrada o Salida.", StatusCodes.Status400BadRequest);

        if (request.Cantidad <= 0)
            throw new AppException("La cantidad debe ser mayor a cero.", StatusCodes.Status400BadRequest);

        var proveedor = await RequireProveedorSiCorrespondeAsync(tipo, request.ProveedorId, cancellationToken);

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
            RegistradoPorUsuario = usuario,
            ProveedorId = proveedor?.Id,
            Proveedor = proveedor
        };

        item.CantidadActual = nuevoSaldo;
        await _movimientos.AddAsync(movimiento, cancellationToken);
        await _items.UpdateAsync(item, cancellationToken);

        if (!estabaEnAlerta && EstaEnAlerta(item))
            await GenerarAlertasStockAsync(item, movimiento, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapMovimiento(movimiento, usuario.Nombre);
    }

    /// <summary>
    /// Un aviso por admin activo. Cero destinatarios no aborta el movimiento.
    /// El alta, la edición del mínimo y una salida rechazada no llegan acá.
    /// </summary>
    private async Task GenerarAlertasStockAsync(
        StockItem item,
        MovimientoStock movimiento,
        CancellationToken cancellationToken)
    {
        var admins = await _usuarios.SearchAsync(
            new UsuarioFilterQuery { Rol = RolesSistema.Admin, Estado = UsuarioEstado.Activo },
            cancellationToken);

        var mensaje = MensajeStockAlerta(item);
        foreach (var admin in admins)
        {
            await _notificaciones.AddAsync(new Notificacion
            {
                UsuarioId = admin.Id,
                Titulo = TituloStockAlerta,
                Mensaje = mensaje,
                Tipo = TipoNotificacion.StockAlerta,
                Leida = false,
                FechaUtc = movimiento.FechaUtc,
                StockItemId = item.Id,
                StockItem = item,
                MovimientoStock = movimiento
            }, cancellationToken);
        }
    }

    private static string MensajeStockAlerta(StockItem item)
    {
        var mensaje =
            $"{item.Nombre} quedó con saldo {FormatoCantidad(item.CantidadActual)} {item.Unidad} (mínimo {FormatoCantidad(item.StockMinimo)}).";
        return mensaje.Length <= 500 ? mensaje : mensaje[..500];
    }

    private static string FormatoCantidad(decimal value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

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

    /// <summary>
    /// Null no vincula proveedor. Salida con id rechaza. Entrada exige proveedor existente y activo.
    /// Se resuelve antes de tocar el saldo.
    /// </summary>
    private async Task<Proveedor?> RequireProveedorSiCorrespondeAsync(
        TipoMovimientoStock tipo,
        int? proveedorId,
        CancellationToken cancellationToken)
    {
        if (!proveedorId.HasValue)
            return null;

        if (tipo == TipoMovimientoStock.Salida)
            throw new AppException("Una salida no lleva proveedor.", StatusCodes.Status400BadRequest);

        var proveedor = await _proveedores.GetByIdAsync(proveedorId.Value, cancellationToken);
        if (proveedor is null || !proveedor.Activo)
            throw new AppException("El proveedor no existe o no está activo.", StatusCodes.Status400BadRequest);

        return proveedor;
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
        RegistradoPorNombre = registradoPorNombre,
        ProveedorId = movimiento.ProveedorId,
        ProveedorNombre = movimiento.Proveedor?.Nombre
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
