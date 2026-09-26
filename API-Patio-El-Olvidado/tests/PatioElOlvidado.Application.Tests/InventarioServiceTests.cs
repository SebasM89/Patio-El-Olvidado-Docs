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
            new UnitOfWork(db));

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

    private static decimal Saldo(IEnumerable<MovimientoStock> movimientos)
        => movimientos.Where(m => m.Tipo == TipoMovimientoStock.Entrada).Sum(m => m.Cantidad)
           - movimientos.Where(m => m.Tipo == TipoMovimientoStock.Salida).Sum(m => m.Cantidad);
}
