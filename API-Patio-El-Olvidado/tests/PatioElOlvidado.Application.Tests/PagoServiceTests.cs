using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class PagoServiceTests
{
    private static async Task<(PagoService Pagos, PedidoService Pedidos, AppDbContext Db, Producto Producto, Pedido PedidoListo)> CreateSutWithPedidoListoAsync(
        decimal cantidad = 2)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var producto = new Producto
        {
            Nombre = "Empanada",
            Precio = 1000m,
            Categoria = "Entradas",
            Activo = true
        };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        var pedidoService = new PedidoService(
            new PedidoRepository(db),
            new ProductoRepository(db),
            new UnitOfWork(db));

        var created = await pedidoService.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = producto.Id, Cantidad = (int)cantidad }]
        }, creadoPorUsuarioId: 1);

        await pedidoService.CambiarEstadoAsync(
            created.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Listo },
            1,
            RolesSistema.Admin);

        var pedido = await db.Pedidos.FirstAsync(p => p.Id == created.Id);

        var pagoService = new PagoService(
            new PagoRepository(db),
            new CajaRepository(db),
            new PedidoRepository(db),
            new UnitOfWork(db));

        return (pagoService, pedidoService, db, producto, pedido);
    }

    [Fact]
    public async Task Create_DivisionDeCuenta_HastaTotal_Ok()
    {
        var (pagos, _, db, _, pedido) = await CreateSutWithPedidoListoAsync();
        Assert.Equal(2000m, pedido.Total);

        var p1 = await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Efectivo,
            Monto = 800m
        });
        var p2 = await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Tarjeta,
            Monto = 1200m
        });

        Assert.Equal(PagoEstado.Completado, p1.Estado);
        Assert.Equal(PagoEstado.Completado, p2.Estado);
        Assert.Equal(2, await db.Pagos.CountAsync());
        Assert.Equal(2000m, await db.Pagos.Where(p => p.Estado == PagoEstado.Completado).SumAsync(p => p.Monto));
    }

    [Fact]
    public async Task Create_ExcedeTotal_Throws409()
    {
        var (pagos, _, _, _, pedido) = await CreateSutWithPedidoListoAsync();

        await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Efectivo,
            Monto = 1500m
        });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            pagos.CreateAsync(new CreatePagoRequest
            {
                PedidoId = pedido.Id,
                Metodo = PagoMetodo.Tarjeta,
                Monto = 600m
            }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Create_RN08_UpsertCajaIncrementaPorMetodo()
    {
        var (pagos, _, db, _, pedido) = await CreateSutWithPedidoListoAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Efectivo,
            Monto = 500m
        });
        await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Tarjeta,
            Monto = 700m
        });
        await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Transferencia,
            Monto = 300m
        });

        var caja = await db.Cajas.SingleAsync(c => c.Fecha == hoy);
        Assert.Equal(500m, caja.TotalEfectivo);
        Assert.Equal(700m, caja.TotalTarjeta);
        Assert.Equal(300m, caja.TotalTransferencia);
        Assert.Equal(1500m, caja.Total);
    }

    [Fact]
    public async Task Create_PedidoEnPreparacion_Throws409()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var producto = new Producto
        {
            Nombre = "Bebida",
            Precio = 500m,
            Categoria = "Bebidas",
            Activo = true
        };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        var pedidoService = new PedidoService(
            new PedidoRepository(db),
            new ProductoRepository(db),
            new UnitOfWork(db));
        var created = await pedidoService.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = producto.Id, Cantidad = 1 }]
        }, 1);

        var pagoService = new PagoService(
            new PagoRepository(db),
            new CajaRepository(db),
            new PedidoRepository(db),
            new UnitOfWork(db));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            pagoService.CreateAsync(new CreatePagoRequest
            {
                PedidoId = created.Id,
                Metodo = PagoMetodo.Efectivo,
                Monto = 500m
            }));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Anular_RevierteCaja_RN08()
    {
        var (pagos, _, db, _, pedido) = await CreateSutWithPedidoListoAsync();

        var pago = await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Tarjeta,
            Monto = 1000m
        });

        var anulado = await pagos.AnularAsync(pago.Id);
        Assert.Equal(PagoEstado.Anulado, anulado.Estado);

        var caja = await db.Cajas.SingleAsync();
        Assert.Equal(0m, caja.TotalTarjeta);
    }

    [Fact]
    public async Task Create_PedidoEntregado_Ok()
    {
        var (pagos, pedidos, _, _, pedido) = await CreateSutWithPedidoListoAsync(cantidad: 1);

        await pedidos.CambiarEstadoAsync(
            pedido.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Entregado },
            1,
            RolesSistema.Admin);

        var pago = await pagos.CreateAsync(new CreatePagoRequest
        {
            PedidoId = pedido.Id,
            Metodo = PagoMetodo.Transferencia,
            Monto = 1000m
        });

        Assert.Equal(PagoEstado.Completado, pago.Estado);
        Assert.Equal(1000m, pago.Monto);
    }
}
