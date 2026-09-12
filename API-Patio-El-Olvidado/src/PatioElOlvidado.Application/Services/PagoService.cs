using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class PagoService : IPagoService
{
    public const decimal MontoTolerance = 0.01m;

    private readonly IPagoRepository _pagos;
    private readonly ICajaRepository _cajas;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _unitOfWork;

    public PagoService(
        IPagoRepository pagos,
        ICajaRepository cajas,
        IPedidoRepository pedidos,
        IClienteRepository clientes,
        IUnitOfWork unitOfWork)
    {
        _pagos = pagos;
        _cajas = cajas;
        _pedidos = pedidos;
        _clientes = clientes;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var pago = await _pagos.GetByIdAsync(id, cancellationToken);
        return pago is null ? null : Map(pago);
    }

    public async Task<IReadOnlyList<PagoDto>> ListByPedidoIdAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
    {
        var items = await _pagos.ListByPedidoIdAsync(pedidoId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PagoDto> CreateAsync(
        CreatePagoRequest request,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _pedidos.GetByIdWithDetallesAsync(request.PedidoId, cancellationToken)
            ?? throw new AppException("Pedido no encontrado.", StatusCodes.Status404NotFound);

        EnsurePedidoElegible(pedido);

        var metodo = request.Metodo.Trim();
        var monto = decimal.Round(request.Monto, 2, MidpointRounding.AwayFromZero);
        if (monto <= 0)
            throw new AppException("El monto debe ser mayor a cero.", StatusCodes.Status400BadRequest);

        var sumCompletados = await _pagos.SumCompletadosByPedidoIdAsync(pedido.Id, cancellationToken);
        if (sumCompletados + monto - pedido.Total > MontoTolerance)
            throw new AppException(
                $"La suma de pagos completados no puede superar el total del pedido ({pedido.Total:0.00}).",
                StatusCodes.Status409Conflict);

        var fechaPago = DateTime.UtcNow;
        var fechaCaja = DateOnly.FromDateTime(fechaPago);
        var caja = await UpsertCajaIncrementAsync(fechaCaja, metodo, monto, cancellationToken);

        var pago = new Pago
        {
            PedidoId = pedido.Id,
            Metodo = metodo,
            Estado = PagoEstado.Completado,
            Monto = monto,
            FechaPago = fechaPago,
            Caja = caja
        };

        await _pagos.AddAsync(pago, cancellationToken);

        // RN-05: visita ++ una sola vez al pasar a totalmente cobrado
        var nuevaSuma = sumCompletados + monto;
        await ContabilizarVisitaSiCorrespondeAsync(pedido, sumCompletados, nuevaSuma, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(pago);
    }

    public async Task<PagoDto> AnularAsync(int id, CancellationToken cancellationToken = default)
    {
        var pago = await _pagos.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Pago no encontrado.", StatusCodes.Status404NotFound);

        if (pago.Estado == PagoEstado.Anulado)
            throw new AppException("El pago ya está anulado.", StatusCodes.Status409Conflict);

        if (pago.Estado != PagoEstado.Completado)
            throw new AppException("Solo se pueden anular pagos Completados.", StatusCodes.Status409Conflict);

        var caja = await _cajas.GetByIdAsync(pago.CajaId, cancellationToken)
            ?? throw new AppException(
                "No se encontró la caja del pago para revertir (RN-08).",
                StatusCodes.Status409Conflict);

        RevertirMontoCaja(caja, pago.Metodo, pago.Monto);
        await _cajas.UpdateAsync(caja, cancellationToken);

        pago.Estado = PagoEstado.Anulado;
        await _pagos.UpdateAsync(pago, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(pago);
    }

    public async Task<CajaDiaDto> GetCajaHoyAsync(CancellationToken cancellationToken = default)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var caja = await _cajas.GetByFechaAsync(hoy, cancellationToken);
        if (caja is null)
        {
            return new CajaDiaDto
            {
                Fecha = hoy,
                TotalEfectivo = 0,
                TotalTarjeta = 0,
                TotalTransferencia = 0,
                Total = 0
            };
        }

        return MapCaja(caja);
    }

    /// <summary>
    /// Incrementa Cliente.Visitas al completar el cobro; idempotente vía Pedido.VisitaContabilizada.
    /// Anulación no decrementa visitas (MVP).
    /// </summary>
    private async Task ContabilizarVisitaSiCorrespondeAsync(
        Pedido pedido,
        decimal sumAntes,
        decimal sumDespues,
        CancellationToken cancellationToken)
    {
        if (!pedido.ClienteId.HasValue || pedido.VisitaContabilizada)
            return;

        var estabaCompleto = sumAntes + MontoTolerance >= pedido.Total;
        var quedaCompleto = sumDespues + MontoTolerance >= pedido.Total;
        if (estabaCompleto || !quedaCompleto)
            return;

        var cliente = await _clientes.GetByIdAsync(pedido.ClienteId.Value, cancellationToken);
        if (cliente is null)
            return;

        cliente.Visitas += 1;
        await _clientes.UpdateAsync(cliente, cancellationToken);

        pedido.VisitaContabilizada = true;
        await _pedidos.UpdateAsync(pedido, cancellationToken);
    }

    private async Task<Caja> UpsertCajaIncrementAsync(
        DateOnly fecha,
        string metodo,
        decimal monto,
        CancellationToken cancellationToken)
    {
        var caja = await _cajas.GetByFechaAsync(fecha, cancellationToken);
        if (caja is null)
        {
            caja = new Caja
            {
                Fecha = fecha,
                TotalEfectivo = 0,
                TotalTarjeta = 0,
                TotalTransferencia = 0
            };
            IncrementarMontoCaja(caja, metodo, monto);
            await _cajas.AddAsync(caja, cancellationToken);
            return caja;
        }

        IncrementarMontoCaja(caja, metodo, monto);
        await _cajas.UpdateAsync(caja, cancellationToken);
        return caja;
    }

    private static void EnsurePedidoElegible(Pedido pedido)
    {
        if (pedido.Estado is PedidoEstado.Listo or PedidoEstado.Entregado)
            return;

        throw new AppException(
            $"Solo se puede cobrar un pedido en estado Listo o Entregado (actual: {pedido.Estado}).",
            StatusCodes.Status409Conflict);
    }

    private static void IncrementarMontoCaja(Caja caja, string metodo, decimal monto)
    {
        switch (metodo)
        {
            case PagoMetodo.Efectivo:
                caja.TotalEfectivo += monto;
                break;
            case PagoMetodo.Tarjeta:
                caja.TotalTarjeta += monto;
                break;
            case PagoMetodo.Transferencia:
                caja.TotalTransferencia += monto;
                break;
            default:
                throw new AppException("Método de pago inválido.", StatusCodes.Status400BadRequest);
        }
    }

    private static void RevertirMontoCaja(Caja caja, string metodo, decimal monto)
    {
        switch (metodo)
        {
            case PagoMetodo.Efectivo:
                caja.TotalEfectivo = Math.Max(0, caja.TotalEfectivo - monto);
                break;
            case PagoMetodo.Tarjeta:
                caja.TotalTarjeta = Math.Max(0, caja.TotalTarjeta - monto);
                break;
            case PagoMetodo.Transferencia:
                caja.TotalTransferencia = Math.Max(0, caja.TotalTransferencia - monto);
                break;
            default:
                throw new AppException("Método de pago inválido.", StatusCodes.Status400BadRequest);
        }
    }

    private static PagoDto Map(Pago pago) => new()
    {
        Id = pago.Id,
        PedidoId = pago.PedidoId,
        Metodo = pago.Metodo,
        Estado = pago.Estado,
        Monto = pago.Monto,
        FechaPago = pago.FechaPago,
        CajaId = pago.CajaId
    };

    private static CajaDiaDto MapCaja(Caja caja) => new()
    {
        Fecha = caja.Fecha,
        TotalEfectivo = caja.TotalEfectivo,
        TotalTarjeta = caja.TotalTarjeta,
        TotalTransferencia = caja.TotalTransferencia,
        Total = caja.Total
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
