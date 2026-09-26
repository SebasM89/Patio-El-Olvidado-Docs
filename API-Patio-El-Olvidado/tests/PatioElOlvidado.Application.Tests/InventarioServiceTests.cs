using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Inventario;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class InventarioServiceTests
{
    private static (InventarioService Service, AppDbContext Db, Usuario Admin) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var rol = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        db.Roles.Add(rol);
        db.SaveChanges();

        var admin = new Usuario
        {
            Nombre = "Admin Test",
            Email = "admin@test.local",
            PasswordHash = "hash",
            RolId = rol.Id,
            Estado = "Activo"
        };
        db.Usuarios.Add(admin);
        db.SaveChanges();

        var service = new InventarioService(
            new StockItemRepository(db),
            new MovimientoStockRepository(db),
            new UsuarioRepository(db),
            new UnitOfWork(db),
            new ProveedorRepository(db),
            new NotificacionRepository(db));

        return (service, db, admin);
    }

    [Fact]
    public async Task AltaConCantidadInicial_InsertaEntradaYSaldo()
    {
        var (service, db, admin) = CreateSut();

        var dto = await service.CreateAsync(new CreateStockItemRequest
        {
            Nombre = " Harina ",
            Descripcion = "Común",
            Unidad = "Kg",
            StockMinimo = 2,
            CantidadInicial = 2,
            Activo = true
        }, admin.Id);

        Assert.Equal("Harina", dto.Nombre);
        Assert.Equal("Kg", dto.Unidad);
        Assert.Equal(2m, dto.CantidadActual);
        Assert.True(dto.EnAlerta);
        Assert.Null(typeof(StockItemDto).GetProperty("Version"));

        var movs = await db.MovimientosStock.ToListAsync();
        Assert.Single(movs);
        Assert.Equal(TipoMovimientoStock.Entrada, movs[0].Tipo);
        Assert.Equal(2m, movs[0].Cantidad);
        Assert.Equal(InventarioService.MotivoAltaInicial, movs[0].Motivo);
        Assert.Equal(admin.Id, movs[0].RegistradoPorUsuarioId);
        Assert.Null(movs[0].ProveedorId);
        Assert.Equal(dto.CantidadActual, movs.Where(m => m.Tipo == TipoMovimientoStock.Entrada).Sum(m => m.Cantidad)
            - movs.Where(m => m.Tipo == TipoMovimientoStock.Salida).Sum(m => m.Cantidad));
    }

    [Fact]
    public async Task Entrada_AumentaSaldo_YResuelveNombre()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Aceite", "L", stockMinimo: 1), admin.Id);

        var mov = await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Entrada",
            Cantidad = 3,
            Motivo = "Compra"
        }, admin.Id);

        Assert.Equal("Entrada", mov.Tipo);
        Assert.Equal("Admin Test", mov.RegistradoPorNombre);

        var item = await service.GetByIdAsync(creado.Id);
        Assert.Equal(3m, item!.CantidadActual);
        Assert.False(item.EnAlerta);

        var stored = await db.StockItems.SingleAsync();
        var movs = await db.MovimientosStock.Where(m => m.StockItemId == stored.Id).ToListAsync();
        Assert.Equal(stored.CantidadActual, Saldo(movs));
    }

    [Fact]
    public async Task Entrada_SinProveedor_SumaSaldoYDejaProveedorNulo()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Arroz", "Kg"), admin.Id);

        var mov = await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Entrada",
            Cantidad = 5,
            Motivo = "Compra sin proveedor"
        }, admin.Id);

        Assert.Null(mov.ProveedorId);
        Assert.Null(mov.ProveedorNombre);
        Assert.Equal(5m, (await service.GetByIdAsync(creado.Id))!.CantidadActual);

        var stored = await db.MovimientosStock.SingleAsync(m => m.Id == mov.Id);
        Assert.Null(stored.ProveedorId);
    }

    [Fact]
    public async Task Entrada_ConProveedorActivo_SumaSaldoYGuardaFk()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Fideos", "Kg", cantidadInicial: 1), admin.Id);
        var proveedor = new Proveedor { Nombre = "Molino Sur", Activo = true };
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();

        var mov = await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Entrada",
            Cantidad = 4,
            ProveedorId = proveedor.Id
        }, admin.Id);

        Assert.Equal(proveedor.Id, mov.ProveedorId);
        Assert.Equal("Molino Sur", mov.ProveedorNombre);
        Assert.Equal(5m, (await service.GetByIdAsync(creado.Id))!.CantidadActual);

        var stored = await db.MovimientosStock.SingleAsync(m => m.Id == mov.Id);
        Assert.Equal(proveedor.Id, stored.ProveedorId);

        proveedor.Nombre = "Molino Norte";
        await db.SaveChangesAsync();

        var listado = await service.ListMovimientosAsync(creado.Id);
        var entrada = Assert.Single(listado, m => m.Id == mov.Id);
        Assert.Equal(proveedor.Id, entrada.ProveedorId);
        Assert.Equal("Molino Norte", entrada.ProveedorNombre);
    }

    [Fact]
    public async Task Salida_ConProveedor_400_SaldoIntacto()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Aceitunas", "Kg", cantidadInicial: 8), admin.Id);
        var proveedor = new Proveedor { Nombre = "Olivares", Activo = true };
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();
        var antes = await db.MovimientosStock.CountAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Salida", Cantidad = 1, ProveedorId = proveedor.Id },
            admin.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(antes, await db.MovimientosStock.CountAsync());
        Assert.Equal(8m, (await db.StockItems.SingleAsync()).CantidadActual);
    }

    [Fact]
    public async Task Entrada_ProveedorInactivoOInexistente_400_SaldoIntacto()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Yerba", "Kg", cantidadInicial: 3), admin.Id);
        var inactivo = new Proveedor { Nombre = "Baja", Activo = false };
        db.Proveedores.Add(inactivo);
        await db.SaveChangesAsync();
        var antes = await db.MovimientosStock.CountAsync();

        var exInactivo = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Entrada", Cantidad = 2, ProveedorId = inactivo.Id },
            admin.Id));
        var exInexistente = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Entrada", Cantidad = 2, ProveedorId = 9999 },
            admin.Id));

        Assert.Equal(400, exInactivo.StatusCode);
        Assert.Equal(400, exInexistente.StatusCode);
        Assert.Equal(antes, await db.MovimientosStock.CountAsync());
        Assert.Equal(3m, (await db.StockItems.SingleAsync()).CantidadActual);
    }

    [Fact]
    public async Task Salida_DescuentaSaldo()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Azucar", "Kg", cantidadInicial: 10, stockMinimo: 1), admin.Id);

        await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Salida",
            Cantidad = 4,
            Motivo = "Cocina"
        }, admin.Id);

        var item = await service.GetByIdAsync(creado.Id);
        Assert.Equal(6m, item!.CantidadActual);

        var movs = await db.MovimientosStock.Where(m => m.StockItemId == creado.Id).ToListAsync();
        Assert.Equal(2, movs.Count);
        Assert.Equal(item.CantidadActual, Saldo(movs));
    }

    [Fact]
    public async Task SalidaMayorAlSaldo_400_NoInserta()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Sal", "Kg", cantidadInicial: 1), admin.Id);
        var antes = await db.MovimientosStock.CountAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Salida", Cantidad = 2 },
            admin.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(antes, await db.MovimientosStock.CountAsync());
        var stored = await db.StockItems.SingleAsync();
        Assert.Equal(1m, stored.CantidadActual);
    }

    [Fact]
    public async Task Alerta_CuandoCantidadEsMenorOIgualAlMinimo()
    {
        var (service, _, admin) = CreateSut();
        var igual = await service.CreateAsync(Item("Igual", "Unidad", cantidadInicial: 5, stockMinimo: 5), admin.Id);
        var arriba = await service.CreateAsync(Item("Arriba", "Unidad", cantidadInicial: 6, stockMinimo: 5), admin.Id);

        Assert.True(igual.EnAlerta);
        Assert.False(arriba.EnAlerta);

        var alertas = await service.ListAlertasAsync();
        Assert.Contains(alertas, a => a.Id == igual.Id);
        Assert.DoesNotContain(alertas, a => a.Id == arriba.Id);
    }

    [Fact]
    public async Task Inactivo_NoAceptaMovimientos()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Levadura", "Unidad", cantidadInicial: 4, stockMinimo: 10), admin.Id);
        Assert.True(creado.EnAlerta);

        var actualizado = await service.UpdateAsync(creado.Id, new UpdateStockItemRequest
        {
            Nombre = "Levadura",
            StockMinimo = 10,
            Activo = false
        });

        Assert.False(actualizado.Activo);
        Assert.False(actualizado.EnAlerta);
        Assert.Equal(4m, actualizado.CantidadActual);
        Assert.Equal("Unidad", actualizado.Unidad);

        var antes = await db.MovimientosStock.CountAsync();
        var ex = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Entrada", Cantidad = 1 },
            admin.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(antes, await db.MovimientosStock.CountAsync());
        Assert.Equal(4m, (await db.StockItems.SingleAsync()).CantidadActual);
    }

    [Fact]
    public async Task Update_NoCambiaCantidadNiUnidad()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Miel", "Kg", cantidadInicial: 8, stockMinimo: 1), admin.Id);

        await service.UpdateAsync(creado.Id, new UpdateStockItemRequest
        {
            Nombre = "Miel pura",
            Descripcion = "Pote",
            StockMinimo = 3,
            Activo = true
        });

        var stored = await db.StockItems.SingleAsync();
        Assert.Equal("Miel pura", stored.Nombre);
        Assert.Equal(8m, stored.CantidadActual);
        Assert.Equal(UnidadStock.Kg, stored.Unidad);
        Assert.Equal(3m, stored.StockMinimo);
    }

    [Fact]
    public async Task TransicionAAlerta_GeneraAvisoSoloParaAdminsActivos()
    {
        var (service, db, admin) = CreateSut();
        var rolEmpleado = new Rol { Nombre = RolesSistema.Empleado, Descripcion = "Empleado" };
        var rolCliente = new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" };
        db.Roles.AddRange(rolEmpleado, rolCliente);
        await db.SaveChangesAsync();

        var adminDos = UsuarioDe(admin.RolId, "Admin Dos", "admin2@test.local", UsuarioEstado.Activo);
        db.Usuarios.AddRange(
            adminDos,
            UsuarioDe(admin.RolId, "Admin Off", "off@test.local", UsuarioEstado.Inactivo),
            UsuarioDe(admin.RolId, "Admin Block", "block@test.local", UsuarioEstado.Bloqueado),
            UsuarioDe(rolEmpleado.Id, "Emi", "emi@test.local", UsuarioEstado.Activo),
            UsuarioDe(rolCliente.Id, "Cli", "cli@test.local", UsuarioEstado.Activo));
        await db.SaveChangesAsync();

        var creado = await service.CreateAsync(Item("Harina", "Kg", cantidadInicial: 10, stockMinimo: 3), admin.Id);
        Assert.Empty(await db.Notificaciones.ToListAsync());

        var mov = await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Salida",
            Cantidad = 8
        }, admin.Id);

        var avisos = await db.Notificaciones.OrderBy(n => n.UsuarioId).ToListAsync();
        Assert.Equal(2, avisos.Count);
        Assert.Equal(new[] { admin.Id, adminDos.Id }.OrderBy(id => id), avisos.Select(n => n.UsuarioId).OrderBy(id => id));
        Assert.All(avisos, n =>
        {
            Assert.Equal(InventarioService.TituloStockAlerta, n.Titulo);
            Assert.Equal("Harina quedó con saldo 2 Kg (mínimo 3).", n.Mensaje);
            Assert.Equal(TipoNotificacion.StockAlerta, n.Tipo);
            Assert.False(n.Leida);
            Assert.Null(n.LeidaUtc);
            Assert.Equal(creado.Id, n.StockItemId);
            Assert.Equal(mov.Id, n.MovimientoStockId);
            Assert.True(n.MovimientoStockId > 0);
        });
    }

    [Fact]
    public async Task YaEnAlerta_NoDuplicaAviso()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Harina", "Kg", cantidadInicial: 10, stockMinimo: 3), admin.Id);

        await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Salida",
            Cantidad = 8
        }, admin.Id);
        Assert.Equal(1, await db.Notificaciones.CountAsync());

        await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Salida",
            Cantidad = 1
        }, admin.Id);

        Assert.Equal(1, await db.Notificaciones.CountAsync());
        var item = await service.GetByIdAsync(creado.Id);
        Assert.True(item!.EnAlerta);
        Assert.Equal(1m, item.CantidadActual);
    }

    [Fact]
    public async Task SalidaRechazadaPorSaldo_NoGeneraAviso()
    {
        var (service, db, admin) = CreateSut();
        var creado = await service.CreateAsync(Item("Sal", "Kg", cantidadInicial: 4, stockMinimo: 1), admin.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.RegistrarMovimientoAsync(
            creado.Id,
            new RegistrarMovimientoRequest { Tipo = "Salida", Cantidad = 9 },
            admin.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(await db.Notificaciones.ToListAsync());
        Assert.Equal(4m, (await db.StockItems.SingleAsync()).CantidadActual);
    }

    [Fact]
    public async Task EntradaSobreMinimo_AltaYEdicionDeMinimo_NoGeneranAviso()
    {
        var (service, db, admin) = CreateSut();

        var alta = await service.CreateAsync(Item("Aceite", "L", cantidadInicial: 1, stockMinimo: 5), admin.Id);
        Assert.True(alta.EnAlerta);
        Assert.Empty(await db.Notificaciones.ToListAsync());

        var arriba = await service.CreateAsync(Item("Arroz", "Kg", cantidadInicial: 10, stockMinimo: 2), admin.Id);
        await service.RegistrarMovimientoAsync(arriba.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Entrada",
            Cantidad = 1
        }, admin.Id);
        Assert.False((await service.GetByIdAsync(arriba.Id))!.EnAlerta);

        await service.UpdateAsync(arriba.Id, new UpdateStockItemRequest
        {
            Nombre = "Arroz",
            StockMinimo = 20,
            Activo = true
        });
        Assert.True((await service.GetByIdAsync(arriba.Id))!.EnAlerta);
        Assert.Empty(await db.Notificaciones.ToListAsync());
    }

    [Fact]
    public async Task SinAdminsActivos_ElMovimientoIgualQuedaRegistrado()
    {
        var (service, db, admin) = CreateSut();
        admin.Estado = UsuarioEstado.Inactivo;
        await db.SaveChangesAsync();

        var creado = await service.CreateAsync(Item("Fideos", "Unidad", cantidadInicial: 5, stockMinimo: 1), admin.Id);
        var mov = await service.RegistrarMovimientoAsync(creado.Id, new RegistrarMovimientoRequest
        {
            Tipo = "Salida",
            Cantidad = 5
        }, admin.Id);

        Assert.Equal(0m, (await service.GetByIdAsync(creado.Id))!.CantidadActual);
        Assert.True(mov.Id > 0);
        Assert.Empty(await db.Notificaciones.ToListAsync());
    }

    [Fact]
    public void Dtos_NoExponenVersion()
    {
        Assert.Null(typeof(StockItemDto).GetProperty("Version"));
        Assert.Null(typeof(MovimientoStockDto).GetProperty("Version"));
        Assert.Null(typeof(UpdateStockItemRequest).GetProperty("CantidadActual"));
        Assert.Null(typeof(UpdateStockItemRequest).GetProperty("Unidad"));
        Assert.Null(typeof(UpdateStockItemRequest).GetProperty("CantidadInicial"));
    }

    private static CreateStockItemRequest Item(
        string nombre,
        string unidad,
        decimal cantidadInicial = 0,
        decimal stockMinimo = 0)
        => new()
        {
            Nombre = nombre,
            Unidad = unidad,
            CantidadInicial = cantidadInicial,
            StockMinimo = stockMinimo,
            Activo = true
        };

    private static Usuario UsuarioDe(int rolId, string nombre, string email, string estado) => new()
    {
        Nombre = nombre,
        Email = email,
        PasswordHash = "hash",
        RolId = rolId,
        Estado = estado
    };

    private static decimal Saldo(IEnumerable<MovimientoStock> movimientos)
        => movimientos.Where(m => m.Tipo == TipoMovimientoStock.Entrada).Sum(m => m.Cantidad)
           - movimientos.Where(m => m.Tipo == TipoMovimientoStock.Salida).Sum(m => m.Cantidad);
}
