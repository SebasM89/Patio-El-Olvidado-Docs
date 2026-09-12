using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Empleados;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class EmpleadoServiceTests
{
    private static (EmpleadoService Service, AppDbContext Db, Usuario UsuarioEmpleado) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var rolEmpleado = new Rol { Nombre = RolesSistema.Empleado, Descripcion = "Empleado" };
        var rolAdmin = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        var rolCliente = new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" };
        db.Roles.AddRange(rolEmpleado, rolAdmin, rolCliente);
        db.SaveChanges();

        var usuarioEmpleado = new Usuario
        {
            Nombre = "User Empleado",
            Email = "e@test.local",
            PasswordHash = "x",
            RolId = rolEmpleado.Id,
            Estado = UsuarioEstado.Activo,
            Rol = rolEmpleado
        };
        var usuarioAdmin = new Usuario
        {
            Nombre = "User Admin",
            Email = "a@test.local",
            PasswordHash = "x",
            RolId = rolAdmin.Id,
            Estado = UsuarioEstado.Activo,
            Rol = rolAdmin
        };
        db.Usuarios.AddRange(usuarioEmpleado, usuarioAdmin);
        db.SaveChanges();

        var service = new EmpleadoService(
            new EmpleadoRepository(db),
            new FichajeRepository(db),
            new LiquidacionRepository(db),
            new UsuarioRepository(db),
            new UnitOfWork(db));

        return (service, db, usuarioEmpleado);
    }

    [Fact]
    public async Task Create_AndList_Ok()
    {
        var (service, _, _) = CreateSut();

        var created = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Ana",
            TarifaHora = 2000m,
            Activo = true
        });

        Assert.True(created.Id > 0);
        Assert.Equal(0m, created.HorasTrabajadas);

        var list = await service.ListAsync(new EmpleadoFilterQuery { Activo = true });
        Assert.Contains(list, e => e.Id == created.Id);
    }

    [Fact]
    public async Task GetById_EmpleadoRol_SoloPropio_RN07()
    {
        var (service, db, usuario) = CreateSut();
        var propio = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Propio",
            TarifaHora = 1500m,
            UsuarioId = usuario.Id,
            Activo = true
        });
        var ajeno = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Ajeno",
            TarifaHora = 1500m,
            Activo = true
        });

        Assert.NotNull(await service.GetByIdAsync(propio.Id, usuario.Id, RolesSistema.Empleado));
        Assert.Null(await service.GetByIdAsync(ajeno.Id, usuario.Id, RolesSistema.Empleado));
        Assert.NotNull(await service.GetByIdAsync(ajeno.Id, 1, RolesSistema.Admin));
    }

    [Fact]
    public async Task Fichaje_SegundaEntrada_Throws409()
    {
        var (service, _, usuario) = CreateSut();
        var emp = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Ficha",
            TarifaHora = 1000m,
            UsuarioId = usuario.Id,
            Activo = true
        });

        await service.RegistrarEntradaMeAsync(usuario.Id);

        var ex = await Assert.ThrowsAsync<AppException>(
            () => service.RegistrarEntradaMeAsync(usuario.Id));
        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("abierto", ex.Message, StringComparison.OrdinalIgnoreCase);
        _ = emp;
    }

    [Fact]
    public async Task Fichaje_SalidaSinAbierto_Throws409()
    {
        var (service, _, usuario) = CreateSut();
        await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "SinAbierto",
            TarifaHora = 1000m,
            UsuarioId = usuario.Id,
            Activo = true
        });

        var ex = await Assert.ThrowsAsync<AppException>(
            () => service.RegistrarSalidaMeAsync(usuario.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Fichaje_Cierre_IncrementaHorasTrabajadas()
    {
        var (service, db, usuario) = CreateSut();
        var emp = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Cierre",
            TarifaHora = 1000m,
            UsuarioId = usuario.Id,
            Activo = true
        });

        await service.RegistrarEntradaMeAsync(usuario.Id);
        var abierto = await db.Fichajes.FirstAsync(f => f.EmpleadoId == emp.Id);
        abierto.EntradaUtc = DateTime.UtcNow.AddHours(-2);
        await db.SaveChangesAsync();

        var cerrado = await service.RegistrarSalidaMeAsync(usuario.Id);
        Assert.NotNull(cerrado.SalidaUtc);
        Assert.True(cerrado.Horas > 0);

        var me = await service.GetMeAsync(usuario.Id);
        Assert.NotNull(me);
        Assert.Equal(cerrado.Horas, me!.HorasTrabajadas);
    }

    [Fact]
    public async Task Liquidacion_PreviewYGenerar_Ok()
    {
        var (service, db, usuario) = CreateSut();
        var admin = await db.Usuarios.FirstAsync(u => u.Email == "a@test.local");
        var emp = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Liq",
            TarifaHora = 2000m,
            UsuarioId = usuario.Id,
            Activo = true
        });

        await service.RegistrarEntradaMeAsync(usuario.Id);
        var abierto = await db.Fichajes.FirstAsync(f => f.EmpleadoId == emp.Id);
        abierto.EntradaUtc = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        await service.RegistrarSalidaMeAsync(usuario.Id);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var preview = await service.PreviewLiquidacionAsync(emp.Id, hoy, hoy);
        Assert.True(preview.Horas > 0);
        Assert.Equal(FichajeRulesExpected(preview.Horas, 2000m), preview.Monto);

        var generada = await service.GenerarLiquidacionAsync(
            emp.Id,
            new CreateLiquidacionRequest { PeriodoDesde = hoy, PeriodoHasta = hoy },
            admin.Id);

        Assert.Equal(preview.Horas, generada.Horas);
        Assert.Equal(2000m, generada.TarifaHoraSnapshot);
        Assert.Equal(preview.Monto, generada.Monto);

        var historial = await service.ListMeLiquidacionesAsync(usuario.Id);
        Assert.Contains(historial, l => l.Id == generada.Id);
    }

    [Fact]
    public async Task Create_UsuarioNoEmpleado_Throws400()
    {
        var (service, db, _) = CreateSut();
        var clienteRol = await db.Roles.FirstAsync(r => r.Nombre == RolesSistema.Cliente);
        var usuarioCliente = new Usuario
        {
            Nombre = "Cli",
            Email = "cli@test.local",
            PasswordHash = "x",
            RolId = clienteRol.Id,
            Estado = UsuarioEstado.Activo,
            Rol = clienteRol
        };
        db.Usuarios.Add(usuarioCliente);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "BadLink",
            TarifaHora = 1000m,
            UsuarioId = usuarioCliente.Id,
            Activo = true
        }));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDelete_ActivoFalse()
    {
        var (service, db, _) = CreateSut();
        var created = await service.CreateAsync(new CreateEmpleadoRequest
        {
            Nombre = "Baja",
            TarifaHora = 1000m,
            Activo = true
        });

        await service.DeleteAsync(created.Id);
        var entity = await db.Empleados.FirstAsync(e => e.Id == created.Id);
        Assert.False(entity.Activo);
    }

    private static decimal FichajeRulesExpected(decimal horas, decimal tarifa)
        => decimal.Round(horas * tarifa, 2, MidpointRounding.AwayFromZero);
}
