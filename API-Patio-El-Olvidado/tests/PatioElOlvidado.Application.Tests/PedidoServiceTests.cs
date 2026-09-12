using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class PedidoServiceTests
{
    private static (PedidoService Service, AppDbContext Db, Producto Activo, Producto Inactivo) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var activo = new Producto
        {
            Nombre = "Empanada",
            Precio = 1000m,
            Categoria = "Entradas",
            Activo = true
        };
        var inactivo = new Producto
        {
            Nombre = "Plato viejo",
            Precio = 500m,
            Categoria = "Otros",
            Activo = false
        };
        db.Productos.AddRange(activo, inactivo);
        db.SaveChanges();

        var service = new PedidoService(
            new PedidoRepository(db),
            new ProductoRepository(db),
            new ClienteRepository(db),
            new UnitOfWork(db));
        return (service, db, activo, inactivo);
    }

    [Fact]
    public async Task Create_CalculatesTotales_WithSnapshotPrecio()
    {
        var (service, _, activo, _) = CreateSut();

        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles =
            [
                new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 2 },
                new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }
            ]
        }, creadoPorUsuarioId: 10);

        Assert.Equal(PedidoEstado.EnPreparacion, created.Estado);
        Assert.Equal(3000m, created.Subtotal);
        Assert.Equal(3000m, created.Total);
        Assert.Equal(2, created.Detalles.Count);
        Assert.All(created.Detalles, d => Assert.Equal(1000m, d.PrecioUnitario));
        Assert.Equal(10, created.CreadoPorUsuarioId);
    }

    [Fact]
    public async Task Create_ProductoInactivo_Throws400()
    {
        var (service, _, _, inactivo) = CreateSut();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreatePedidoRequest
            {
                Tipo = PedidoTipo.ParaLlevar,
                Detalles = [new DetallePedidoLineRequest { ProductoId = inactivo.Id, Cantidad = 1 }]
            }, creadoPorUsuarioId: 1));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Update_WhenNotEnPreparacion_Throws409_RN04()
    {
        var (service, _, activo, _) = CreateSut();
        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
        }, creadoPorUsuarioId: 1);

        await service.CambiarEstadoAsync(
            created.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Listo },
            usuarioId: 1,
            rol: RolesSistema.Admin);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(
                created.Id,
                new UpdatePedidoRequest
                {
                    Tipo = PedidoTipo.ParaLlevar,
                    Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 2 }]
                },
                usuarioId: 1,
                rol: RolesSistema.Admin));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("RN-04", ex.Message);
    }

    [Fact]
    public async Task CambiarEstado_Staff_TransitionsOk()
    {
        var (service, _, activo, _) = CreateSut();
        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
        }, creadoPorUsuarioId: 1);

        var listo = await service.CambiarEstadoAsync(
            created.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Listo },
            1,
            RolesSistema.Empleado);
        Assert.Equal(PedidoEstado.Listo, listo.Estado);

        var entregado = await service.CambiarEstadoAsync(
            created.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Entregado },
            1,
            RolesSistema.Empleado);
        Assert.Equal(PedidoEstado.Entregado, entregado.Estado);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CambiarEstadoAsync(
                created.Id,
                new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Cancelado },
                1,
                RolesSistema.Admin));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Cliente_CannotMarkListo_AndOnlySeesOwn()
    {
        var (service, _, activo, _) = CreateSut();
        var propio = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.ParaLlevar,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
        }, creadoPorUsuarioId: 20);

        await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
        }, creadoPorUsuarioId: 99);

        var list = await service.ListAsync(new PedidoFilterQuery(), usuarioId: 20, rol: RolesSistema.Cliente);
        Assert.Single(list);
        Assert.Equal(propio.Id, list[0].Id);

        var allStaff = await service.ListAsync(new PedidoFilterQuery(), 1, RolesSistema.Admin);
        var notOwn = allStaff.First(p => p.CreadoPorUsuarioId != 20);
        Assert.Null(await service.GetByIdAsync(notOwn.Id, 20, RolesSistema.Cliente));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CambiarEstadoAsync(
                propio.Id,
                new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Listo },
                20,
                RolesSistema.Cliente));
        Assert.Equal(403, ex.StatusCode);

        var cancelado = await service.CambiarEstadoAsync(
            propio.Id,
            new CambiarEstadoPedidoRequest { Estado = PedidoEstado.Cancelado },
            20,
            RolesSistema.Cliente);
        Assert.Equal(PedidoEstado.Cancelado, cancelado.Estado);
    }

    [Fact]
    public async Task Update_EnPreparacion_RecalculatesTotales()
    {
        var (service, _, activo, _) = CreateSut();
        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
        }, creadoPorUsuarioId: 5);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdatePedidoRequest
            {
                Tipo = PedidoTipo.ParaLlevar,
                Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 3 }]
            },
            5,
            RolesSistema.Cliente);

        Assert.Equal(PedidoTipo.ParaLlevar, updated.Tipo);
        Assert.Equal(3000m, updated.Total);
        Assert.Single(updated.Detalles);
        Assert.Equal(3, updated.Detalles[0].Cantidad);
    }

    [Fact]
    public async Task Create_ClienteVisitas4_AplicaDescuento10_RN05()
    {
        var (service, db, activo, _) = CreateSut();
        var cliente = new Cliente
        {
            Nombre = "Fiel",
            Telefono = "123",
            Visitas = 4,
            Activo = true
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            ClienteId = cliente.Id,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 2 }]
        }, creadoPorUsuarioId: 1);

        Assert.Equal(2000m, created.Subtotal);
        Assert.Equal(1800m, created.Total);
        Assert.Equal(200m, created.DescuentoMonto);
        Assert.True(created.DescuentoAplicado);
    }

    [Fact]
    public async Task Create_ClienteVisitas3_SinDescuento_RN05()
    {
        var (service, db, activo, _) = CreateSut();
        var cliente = new Cliente
        {
            Nombre = "Casi",
            Telefono = "456",
            Visitas = 3,
            Activo = true
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var created = await service.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            ClienteId = cliente.Id,
            Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 2 }]
        }, creadoPorUsuarioId: 1);

        Assert.Equal(2000m, created.Subtotal);
        Assert.Equal(2000m, created.Total);
        Assert.Equal(0m, created.DescuentoMonto);
        Assert.False(created.DescuentoAplicado);
    }

    [Fact]
    public async Task Create_ClienteIdInexistente_Throws400()
    {
        var (service, _, activo, _) = CreateSut();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreatePedidoRequest
            {
                Tipo = PedidoTipo.Local,
                ClienteId = 9999,
                Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
            }, creadoPorUsuarioId: 1));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Create_ClienteInactivo_Throws400()
    {
        var (service, db, activo, _) = CreateSut();
        var cliente = new Cliente
        {
            Nombre = "Baja",
            Telefono = "789",
            Visitas = 0,
            Activo = false
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreatePedidoRequest
            {
                Tipo = PedidoTipo.Local,
                ClienteId = cliente.Id,
                Detalles = [new DetallePedidoLineRequest { ProductoId = activo.Id, Cantidad = 1 }]
            }, creadoPorUsuarioId: 1));

        Assert.Equal(400, ex.StatusCode);
    }
}
