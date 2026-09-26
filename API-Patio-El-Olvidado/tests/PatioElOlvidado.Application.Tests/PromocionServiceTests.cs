using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.DTOs.Promociones;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class PromocionServiceTests
{
    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static (PromocionService Service, AppDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var service = new PromocionService(new PromocionRepository(db), new UnitOfWork(db));
        return (service, db);
    }

    private static CreatePromocionRequest Request(
        string nombre,
        string descripcion,
        DateOnly desde,
        DateOnly hasta) => new()
    {
        Nombre = nombre,
        Descripcion = descripcion,
        VigenteDesde = desde,
        VigenteHasta = hasta
    };

    [Fact]
    public async Task AdminCreaVigente_ClienteLaVeEnListaYDetalle()
    {
        var (service, _) = CreateSut();
        var hoy = Hoy();

        var creada = await service.CreateAsync(
            Request("  Almuerzo  ", "  Menú del día  ", hoy, hoy),
            RolesSistema.Admin);

        Assert.True(creada.Activo);
        Assert.Equal("Almuerzo", creada.Nombre);
        Assert.Equal("Menú del día", creada.Descripcion);

        var lista = await service.ListAsync(
            new PromocionFilterQuery { Q = "no-existe", Activo = false, Vigente = false },
            RolesSistema.Cliente);
        var vista = Assert.Single(lista);
        Assert.Equal(creada.Id, vista.Id);

        var detalle = await service.GetByIdAsync(creada.Id, RolesSistema.Cliente);
        Assert.NotNull(detalle);
        Assert.Equal(creada.Id, detalle!.Id);
        Assert.Equal(hoy, detalle.VigenteDesde);
        Assert.Equal(hoy, detalle.VigenteHasta);
    }

    [Fact]
    public async Task Cliente_NoVeInactivasNiFueraDeVigencia_DetalleNull()
    {
        var (service, _) = CreateSut();
        var hoy = Hoy();

        var inactiva = await service.CreateAsync(
            Request("Pausa", "Cubierta pero inactiva", hoy.AddDays(-1), hoy.AddDays(1)),
            RolesSistema.Admin);
        await service.UpdateAsync(inactiva.Id, new UpdatePromocionRequest
        {
            Nombre = inactiva.Nombre,
            Descripcion = inactiva.Descripcion,
            VigenteDesde = inactiva.VigenteDesde,
            VigenteHasta = inactiva.VigenteHasta,
            Activo = false
        }, RolesSistema.Admin);

        var pasada = await service.CreateAsync(
            Request("Ayer", "Ya terminó", hoy.AddDays(-5), hoy.AddDays(-1)),
            RolesSistema.Admin);
        var futura = await service.CreateAsync(
            Request("Mañana", "Todavía no", hoy.AddDays(1), hoy.AddDays(5)),
            RolesSistema.Admin);

        var lista = await service.ListAsync(new PromocionFilterQuery(), RolesSistema.Cliente);
        Assert.Empty(lista);

        Assert.Null(await service.GetByIdAsync(inactiva.Id, RolesSistema.Cliente));
        Assert.Null(await service.GetByIdAsync(pasada.Id, RolesSistema.Cliente));
        Assert.Null(await service.GetByIdAsync(futura.Id, RolesSistema.Cliente));

        Assert.False((await service.GetByIdAsync(inactiva.Id, RolesSistema.Admin))!.Activo);
        Assert.NotNull(await service.GetByIdAsync(pasada.Id, RolesSistema.Admin));
        Assert.NotNull(await service.GetByIdAsync(futura.Id, RolesSistema.Admin));
    }

    [Fact]
    public async Task Admin_FiltraTextoActivoYVigente()
    {
        var (service, _) = CreateSut();
        var hoy = Hoy();

        var vigente = await service.CreateAsync(
            Request("Hoy", "Incluye la palabra horno", hoy, hoy.AddDays(2)),
            RolesSistema.Admin);
        var pasada = await service.CreateAsync(
            Request("Antigua", "Sin esa palabra", hoy.AddDays(-4), hoy.AddDays(-1)),
            RolesSistema.Admin);
        var inactiva = await service.CreateAsync(
            Request("Baja", "Otra cosa", hoy, hoy),
            RolesSistema.Admin);
        await service.DeleteAsync(inactiva.Id, RolesSistema.Admin);

        var porTexto = await service.ListAsync(
            new PromocionFilterQuery { Q = "horno" },
            RolesSistema.Admin);
        Assert.Equal(vigente.Id, Assert.Single(porTexto).Id);

        var porNombre = await service.ListAsync(
            new PromocionFilterQuery { Q = "Antigua" },
            RolesSistema.Admin);
        Assert.Equal(pasada.Id, Assert.Single(porNombre).Id);

        var soloActivas = await service.ListAsync(
            new PromocionFilterQuery { Activo = true },
            RolesSistema.Admin);
        Assert.Contains(soloActivas, p => p.Id == vigente.Id);
        Assert.DoesNotContain(soloActivas, p => p.Id == inactiva.Id);

        var soloInactivas = await service.ListAsync(
            new PromocionFilterQuery { Activo = false },
            RolesSistema.Admin);
        Assert.Equal(inactiva.Id, Assert.Single(soloInactivas).Id);

        var vigentes = await service.ListAsync(
            new PromocionFilterQuery { Vigente = true },
            RolesSistema.Admin);
        Assert.Contains(vigentes, p => p.Id == vigente.Id);
        Assert.Contains(vigentes, p => p.Id == inactiva.Id);
        Assert.DoesNotContain(vigentes, p => p.Id == pasada.Id);

        var noVigentes = await service.ListAsync(
            new PromocionFilterQuery { Vigente = false },
            RolesSistema.Admin);
        Assert.Contains(noVigentes, p => p.Id == pasada.Id);
        Assert.DoesNotContain(noVigentes, p => p.Id == vigente.Id);
        Assert.DoesNotContain(noVigentes, p => p.Id == inactiva.Id);

        var inactivasQueCubrenHoy = await service.ListAsync(
            new PromocionFilterQuery { Activo = false, Vigente = true },
            RolesSistema.Admin);
        Assert.Equal(inactiva.Id, Assert.Single(inactivasQueCubrenHoy).Id);
    }

    [Fact]
    public async Task Empleado_Fallback403()
    {
        var (service, _) = CreateSut();
        var hoy = Hoy();
        var creada = await service.CreateAsync(
            Request("Aviso", "Texto", hoy, hoy),
            RolesSistema.Admin);
        var update = new UpdatePromocionRequest
        {
            Nombre = "Aviso",
            Descripcion = "Texto",
            VigenteDesde = hoy,
            VigenteHasta = hoy,
            Activo = true
        };

        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.ListAsync(new PromocionFilterQuery(), RolesSistema.Empleado))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.GetByIdAsync(creada.Id, RolesSistema.Empleado))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(Request("Otra", "Texto", hoy, hoy), RolesSistema.Empleado))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(creada.Id, update, RolesSistema.Empleado))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.DeleteAsync(creada.Id, RolesSistema.Empleado))).StatusCode);
    }

    [Fact]
    public async Task Cliente_Mutaciones403()
    {
        var (service, _) = CreateSut();
        var hoy = Hoy();
        var creada = await service.CreateAsync(
            Request("Aviso", "Texto", hoy, hoy),
            RolesSistema.Admin);
        var update = new UpdatePromocionRequest
        {
            Nombre = "Aviso",
            Descripcion = "Texto",
            VigenteDesde = hoy,
            VigenteHasta = hoy,
            Activo = true
        };

        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(Request("Otra", "Texto", hoy, hoy), RolesSistema.Cliente))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(creada.Id, update, RolesSistema.Cliente))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<AppException>(() =>
            service.DeleteAsync(creada.Id, RolesSistema.Cliente))).StatusCode);
    }

    [Fact]
    public async Task VigenciaInvertidaYTextosVaciosOLargos_400()
    {
        var (service, db) = CreateSut();
        var hoy = Hoy();

        var invertida = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                Request("Ok", "Ok", hoy, hoy.AddDays(-1)),
                RolesSistema.Admin));
        Assert.Equal(400, invertida.StatusCode);

        var vacio = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(Request("   ", "   ", hoy, hoy), RolesSistema.Admin));
        Assert.Equal(400, vacio.StatusCode);

        var largo = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                Request(new string('n', 101), new string('d', 501), hoy, hoy),
                RolesSistema.Admin));
        Assert.Equal(400, largo.StatusCode);

        var creada = await service.CreateAsync(Request("Ok", "Ok", hoy, hoy), RolesSistema.Admin);
        var updateInvertida = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(creada.Id, new UpdatePromocionRequest
            {
                Nombre = "Ok",
                Descripcion = "Ok",
                VigenteDesde = hoy,
                VigenteHasta = hoy.AddDays(-1),
                Activo = true
            }, RolesSistema.Admin));
        Assert.Equal(400, updateInvertida.StatusCode);
        Assert.Equal(1, await db.Promociones.CountAsync());
    }

    [Fact]
    public void Validadores_RechazanVigenciaInvertidaYTextosVaciosOLargos()
    {
        var hoy = Hoy();
        var create = new CreatePromocionRequestValidator();
        var update = new UpdatePromocionRequestValidator();

        var invertida = create.TestValidate(Request("Ok", "Ok", hoy, hoy.AddDays(-1)));
        invertida.ShouldHaveValidationErrorFor(x => x.VigenteHasta);

        var vacio = create.TestValidate(Request(" ", "", hoy, hoy));
        vacio.ShouldHaveValidationErrorFor(x => x.Nombre);
        vacio.ShouldHaveValidationErrorFor(x => x.Descripcion);

        var largo = create.TestValidate(Request(new string('n', 101), new string('d', 501), hoy, hoy));
        largo.ShouldHaveValidationErrorFor(x => x.Nombre);
        largo.ShouldHaveValidationErrorFor(x => x.Descripcion);

        var updateLargo = update.TestValidate(new UpdatePromocionRequest
        {
            Nombre = new string('n', 101),
            Descripcion = "",
            VigenteDesde = hoy,
            VigenteHasta = hoy.AddDays(-2),
            Activo = true
        });
        updateLargo.ShouldHaveValidationErrorFor(x => x.Nombre);
        updateLargo.ShouldHaveValidationErrorFor(x => x.Descripcion);
        updateLargo.ShouldHaveValidationErrorFor(x => x.VigenteHasta);
    }

    [Fact]
    public async Task BajaLogica_ClienteNoLaVe_AdminLaVeInactiva_SegundaBajaIdempotente()
    {
        var (service, db) = CreateSut();
        var hoy = Hoy();
        var creada = await service.CreateAsync(
            Request("Combo", "Dos platos", hoy.AddDays(-1), hoy.AddDays(1)),
            RolesSistema.Admin);

        await service.DeleteAsync(creada.Id, RolesSistema.Admin);
        await service.DeleteAsync(creada.Id, RolesSistema.Admin);

        var guardada = await db.Promociones.SingleAsync();
        Assert.False(guardada.Activo);

        var admin = await service.GetByIdAsync(creada.Id, RolesSistema.Admin);
        Assert.NotNull(admin);
        Assert.False(admin!.Activo);

        Assert.Empty(await service.ListAsync(new PromocionFilterQuery(), RolesSistema.Cliente));
        Assert.Null(await service.GetByIdAsync(creada.Id, RolesSistema.Cliente));

        var missing = await Assert.ThrowsAsync<AppException>(() =>
            service.DeleteAsync(9999, RolesSistema.Admin));
        Assert.Equal(404, missing.StatusCode);
    }

    [Fact]
    public async Task Promocion_NoAlteraTotalNiVisitas_RN05()
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
        var cliente = new Cliente
        {
            Nombre = "Fiel",
            Telefono = "123",
            Visitas = 4,
            Activo = true
        };
        db.Productos.Add(producto);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var pedidos = new PedidoService(
            new PedidoRepository(db),
            new ProductoRepository(db),
            new ClienteRepository(db),
            new UnitOfWork(db));
        var promociones = new PromocionService(new PromocionRepository(db), new UnitOfWork(db));
        var hoy = Hoy();

        var pedido = await pedidos.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            ClienteId = cliente.Id,
            Detalles = [new DetallePedidoLineRequest { ProductoId = producto.Id, Cantidad = 2 }]
        }, creadoPorUsuarioId: 1);

        Assert.Equal(2000m, pedido.Subtotal);
        Assert.Equal(pedido.Subtotal * 0.9m, pedido.Total);

        var visitas = (await db.Clientes.SingleAsync()).Visitas;
        var total = (await db.Pedidos.SingleAsync()).Total;
        Assert.Equal(4, visitas);
        Assert.Equal(1800m, total);

        var creada = await promociones.CreateAsync(
            Request("No toca el pedido", "Aviso", hoy, hoy),
            RolesSistema.Admin);
        await promociones.DeleteAsync(creada.Id, RolesSistema.Admin);

        Assert.Equal(total, (await db.Pedidos.SingleAsync()).Total);
        Assert.Equal(visitas, (await db.Clientes.SingleAsync()).Visitas);
    }
}
