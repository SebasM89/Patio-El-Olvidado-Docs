using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidos;
    private readonly IProductoRepository _productos;
    private readonly IUnitOfWork _unitOfWork;

    public PedidoService(
        IPedidoRepository pedidos,
        IProductoRepository productos,
        IUnitOfWork unitOfWork)
    {
        _pedidos = pedidos;
        _productos = productos;
        _unitOfWork = unitOfWork;
    }

    public async Task<PedidoDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _pedidos.GetByIdWithDetallesAsync(id, cancellationToken);
        if (pedido is null)
            return null;

        if (IsCliente(rol) && pedido.CreadoPorUsuarioId != usuarioId)
            return null;

        return Map(pedido);
    }

    public async Task<IReadOnlyList<PedidoDto>> ListAsync(
        PedidoFilterQuery filter,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        int? soloPropios = IsCliente(rol) ? usuarioId : null;
        var items = await _pedidos.SearchAsync(filter, soloPropios, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PedidoDto> CreateAsync(
        CreatePedidoRequest request,
        int creadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var lineas = await BuildDetallesAsync(request.Detalles, cancellationToken);
        var (subtotal, total) = CalcularTotales(lineas);

        var pedido = new Pedido
        {
            Tipo = request.Tipo.Trim(),
            Estado = PedidoEstado.EnPreparacion,
            Subtotal = subtotal,
            Total = total,
            ClienteId = request.ClienteId,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            FechaCreacion = DateTime.UtcNow,
            Detalles = lineas
        };

        await _pedidos.AddAsync(pedido, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _pedidos.GetByIdWithDetallesAsync(pedido.Id, cancellationToken)
            ?? throw new AppException("No se pudo cargar el pedido creado.", StatusCodes.Status500InternalServerError);
        return Map(created);
    }

    public async Task<PedidoDto> UpdateAsync(
        int id,
        UpdatePedidoRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _pedidos.GetByIdWithDetallesAsync(id, cancellationToken)
            ?? throw new AppException("Pedido no encontrado.", StatusCodes.Status404NotFound);

        EnsureOwnership(pedido, usuarioId, rol);

        // RN-04: solo se pueden modificar pedidos en EnPreparacion
        if (pedido.Estado != PedidoEstado.EnPreparacion)
            throw new AppException(
                "Solo se pueden modificar pedidos en estado EnPreparacion (RN-04).",
                StatusCodes.Status409Conflict);

        var nuevasLineas = await BuildDetallesAsync(request.Detalles, cancellationToken);
        var (subtotal, total) = CalcularTotales(nuevasLineas);

        _pedidos.RemoveDetalles(pedido.Detalles.ToList());
        pedido.Detalles.Clear();

        pedido.Tipo = request.Tipo.Trim();
        pedido.ClienteId = request.ClienteId;
        pedido.Subtotal = subtotal;
        pedido.Total = total;
        foreach (var linea in nuevasLineas)
            pedido.Detalles.Add(linea);

        await _pedidos.UpdateAsync(pedido, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _pedidos.GetByIdWithDetallesAsync(pedido.Id, cancellationToken)
            ?? throw new AppException("No se pudo cargar el pedido actualizado.", StatusCodes.Status500InternalServerError);
        return Map(updated);
    }

    public async Task<PedidoDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoPedidoRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _pedidos.GetByIdWithDetallesAsync(id, cancellationToken)
            ?? throw new AppException("Pedido no encontrado.", StatusCodes.Status404NotFound);

        EnsureOwnership(pedido, usuarioId, rol);

        var nuevoEstado = request.Estado.Trim();
        EnsureTransicionPermitida(pedido.Estado, nuevoEstado, rol);

        pedido.Estado = nuevoEstado;
        await _pedidos.UpdateAsync(pedido, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(pedido);
    }

    private async Task<List<DetallePedido>> BuildDetallesAsync(
        IEnumerable<DetallePedidoLineRequest> lineRequests,
        CancellationToken cancellationToken)
    {
        var lineas = new List<DetallePedido>();
        foreach (var line in lineRequests)
        {
            var producto = await _productos.GetByIdAsync(line.ProductoId, cancellationToken)
                ?? throw new AppException(
                    $"Producto {line.ProductoId} no encontrado.",
                    StatusCodes.Status400BadRequest);

            if (!producto.Activo)
                throw new AppException(
                    $"El producto '{producto.Nombre}' no está activo.",
                    StatusCodes.Status400BadRequest);

            lineas.Add(new DetallePedido
            {
                ProductoId = producto.Id,
                Cantidad = line.Cantidad,
                PrecioUnitario = producto.Precio
            });
        }

        return lineas;
    }

    private static (decimal Subtotal, decimal Total) CalcularTotales(IEnumerable<DetallePedido> lineas)
    {
        var subtotal = lineas.Sum(l => l.Cantidad * l.PrecioUnitario);
        return (subtotal, subtotal);
    }

    private static void EnsureOwnership(Pedido pedido, int usuarioId, string rol)
    {
        if (IsCliente(rol) && pedido.CreadoPorUsuarioId != usuarioId)
            throw new AppException("Pedido no encontrado.", StatusCodes.Status404NotFound);
    }

    private static void EnsureTransicionPermitida(string estadoActual, string nuevoEstado, string rol)
    {
        if (estadoActual == nuevoEstado)
            throw new AppException("El pedido ya está en ese estado.", StatusCodes.Status409Conflict);

        if (PedidoEstado.IsTerminal(estadoActual))
            throw new AppException(
                $"No se puede cambiar el estado de un pedido {estadoActual}.",
                StatusCodes.Status409Conflict);

        if (IsCliente(rol))
        {
            if (nuevoEstado != PedidoEstado.Cancelado || estadoActual != PedidoEstado.EnPreparacion)
                throw new AppException(
                    "El cliente solo puede cancelar pedidos propios en EnPreparacion.",
                    StatusCodes.Status403Forbidden);
            return;
        }

        // Staff: Admin / Empleado
        var permitida = (estadoActual, nuevoEstado) switch
        {
            (PedidoEstado.EnPreparacion, PedidoEstado.Listo) => true,
            (PedidoEstado.EnPreparacion, PedidoEstado.Cancelado) => true,
            (PedidoEstado.Listo, PedidoEstado.Entregado) => true,
            (PedidoEstado.Listo, PedidoEstado.Cancelado) => true,
            _ => false
        };

        if (!permitida)
            throw new AppException(
                $"Transición de estado no permitida: {estadoActual} → {nuevoEstado}.",
                StatusCodes.Status409Conflict);
    }

    private static bool IsCliente(string rol)
        => string.Equals(rol, RolesSistema.Cliente, StringComparison.Ordinal);

    private static PedidoDto Map(Pedido pedido) => new()
    {
        Id = pedido.Id,
        Tipo = pedido.Tipo,
        Estado = pedido.Estado,
        Subtotal = pedido.Subtotal,
        Total = pedido.Total,
        FechaCreacion = pedido.FechaCreacion,
        ClienteId = pedido.ClienteId,
        CreadoPorUsuarioId = pedido.CreadoPorUsuarioId,
        Detalles = pedido.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new DetallePedidoDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombre = d.Producto?.Nombre,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Cantidad * d.PrecioUnitario
            })
            .ToList()
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
        public const int Status500InternalServerError = 500;
    }
}
