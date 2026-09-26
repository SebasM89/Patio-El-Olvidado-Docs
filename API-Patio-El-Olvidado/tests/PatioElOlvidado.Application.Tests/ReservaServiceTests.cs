using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Reservas;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class ReservaServiceTests
{
    private static readonly DateOnly Fecha = new(2026, 9, 26);
    private static readonly TimeOnly Doce = new(12, 0);
    private static readonly TimeOnly Trece = new(13, 0);
    private static readonly TimeOnly TreceMedia = new(13, 30);
    private static readonly TimeOnly Catorce = new(14, 0);

    private sealed record Seed(
        int AdminId,
        int EmpleadoId,
        int UsuarioClienteAId,
        int UsuarioClienteBId,
        int ClienteAId,
        int ClienteBId,
        int ClienteInactivoId,
        int MesaCap2Id,
        int MesaCap4Id);

    private static (ReservaService Service, AppDbContext Db, Seed Data) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var rolAdmin = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        var rolEmpleado = new Rol { Nombre = RolesSistema.Empleado, Descripcion = "Empleado" };
        var rolCliente = new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" };
        db.Roles.AddRange(rolAdmin, rolEmpleado, rolCliente);
        db.SaveChanges();

        var admin = Usuario("Admin", "admin@test.local", rolAdmin);
        var empleado = Usuario("Empleado", "emp@test.local", rolEmpleado);
        var userA = Usuario("Ana", "ana@test.local", rolCliente);
        var userB = Usuario("Beto", "beto@test.local", rolCliente);
        db.Usuarios.AddRange(admin, empleado, userA, userB);
        db.SaveChanges();

        var clienteA = new Cliente
        {
            Nombre = "Ana",
            Telefono = "111",
            UsuarioId = userA.Id,
            Activo = true
        };
        var clienteB = new Cliente
        {
            Nombre = "Beto",
            Telefono = "222",
            UsuarioId = userB.Id,
            Activo = true
        };
        var inactivo = new Cliente
        {
            Nombre = "Inactivo",
            Telefono = "333",
            Activo = false
        };
        db.Clientes.AddRange(clienteA, clienteB, inactivo);

        var mesa2 = new Mesa { Numero = 1, Capacidad = 2, Ubicacion = "Ventana" };
        var mesa4 = new Mesa { Numero = 2, Capacidad = 4, Ubicacion = "Salón" };
        db.Mesas.AddRange(mesa2, mesa4);
        db.SaveChanges();

        var service = new ReservaService(
            new ReservaRepository(db),
            new MesaRepository(db),
            new ClienteRepository(db),
            new UnitOfWork(db));

        var seed = new Seed(
            admin.Id,
            empleado.Id,
            userA.Id,
            userB.Id,
            clienteA.Id,
            clienteB.Id,
            inactivo.Id,
            mesa2.Id,
            mesa4.Id);

        return (service, db, seed);
    }

    [Fact]
    public async Task Solape_MismaMesaYFecha_Confirmadas_409()
    {
        var (service, db, data) = CreateSut();
        var primera = await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);

        Assert.Equal(nameof(EstadoReserva.Confirmada), primera.Estado);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CrearAsync(service, data, data.MesaCap4Id, new TimeOnly(12, 30), TreceMedia));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(1, await db.Reservas.CountAsync());
    }

    [Fact]
    public async Task OtraMesa_MismaHora_Ok()
    {
        var (service, _, data) = CreateSut();
        var a = await CrearAsync(service, data, data.MesaCap2Id, Doce, Trece, personas: 2);
        var b = await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece, personas: 3);

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(nameof(EstadoReserva.Confirmada), b.Estado);
    }

    [Fact]
    public async Task ExtremosEnContacto_NoSolapa_Ok()
    {
        var (service, _, data) = CreateSut();
        await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);
        var siguiente = await CrearAsync(service, data, data.MesaCap4Id, Trece, Catorce);

        Assert.Equal(Trece, siguiente.HoraInicio);
        Assert.Equal(nameof(EstadoReserva.Confirmada), siguiente.Estado);
    }

    [Fact]
    public async Task Capacidad_IgualOk_Supera400()
    {
        var (service, _, data) = CreateSut();
        var justa = await CrearAsync(service, data, data.MesaCap2Id, Doce, Trece, personas: 2);
        Assert.Equal(2, justa.Personas);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CrearAsync(service, data, data.MesaCap2Id, Catorce, new TimeOnly(15, 0), personas: 3));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Franja_FinNoPosterior_400()
    {
        var (service, db, data) = CreateSut();

        var igual = await Assert.ThrowsAsync<AppException>(() =>
            CrearAsync(service, data, data.MesaCap4Id, Trece, Trece));
        var invertida = await Assert.ThrowsAsync<AppException>(() =>
            CrearAsync(service, data, data.MesaCap4Id, Catorce, Trece));

        Assert.Equal(400, igual.StatusCode);
        Assert.Equal(400, invertida.StatusCode);
        Assert.Equal(0, await db.Reservas.CountAsync());
    }

    [Fact]
    public async Task Cancelada_NoOcupaHorario_Ok()
    {
        var (service, _, data) = CreateSut();
        var original = await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);
        await service.CambiarEstadoAsync(
            original.Id,
            new CambiarEstadoReservaRequest { Estado = nameof(EstadoReserva.Cancelada) },
            data.AdminId,
            RolesSistema.Admin);

        var nueva = await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);
        Assert.Equal(nameof(EstadoReserva.Confirmada), nueva.Estado);
        Assert.NotEqual(original.Id, nueva.Id);
    }

    [Fact]
    public async Task Finalizada_NoOcupaHorario_Ok()
    {
        var (service, _, data) = CreateSut();
        var original = await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);
        var finalizada = await service.CambiarEstadoAsync(
            original.Id,
            new CambiarEstadoReservaRequest { Estado = nameof(EstadoReserva.Finalizada) },
            data.EmpleadoId,
            RolesSistema.Empleado);

        Assert.Equal(nameof(EstadoReserva.Finalizada), finalizada.Estado);

        var nueva = await CrearAsync(service, data, data.MesaCap4Id, new TimeOnly(12, 15), TreceMedia);
        Assert.Equal(nameof(EstadoReserva.Confirmada), nueva.Estado);
    }

    [Fact]
    public async Task Cliente_ListaSoloPropias_IgnoraFiltros()
    {
        var (service, _, data) = CreateSut();
        var propia = await service.CreateAsync(
            Request(data.MesaCap2Id, Doce, Trece, personas: 2),
            data.UsuarioClienteAId,
            RolesSistema.Cliente);
        await service.CreateAsync(
            Request(data.MesaCap4Id, Doce, Trece, personas: 2, clienteId: data.ClienteBId),
            data.AdminId,
            RolesSistema.Admin);

        Assert.Equal(data.ClienteAId, propia.ClienteId);

        var list = await service.ListAsync(
            new ReservaFilterQuery { Fecha = Fecha, MesaId = data.MesaCap4Id },
            data.UsuarioClienteAId,
            RolesSistema.Cliente);

        var unica = Assert.Single(list);
        Assert.Equal(propia.Id, unica.Id);
    }

    [Fact]
    public async Task Cliente_Ajena403_NoFinaliza_CancelaPropia()
    {
        var (service, db, data) = CreateSut();
        var ajena = await service.CreateAsync(
            Request(data.MesaCap4Id, Doce, Trece, personas: 2, clienteId: data.ClienteBId),
            data.AdminId,
            RolesSistema.Admin);
        var propia = await service.CreateAsync(
            Request(data.MesaCap2Id, Doce, Trece, personas: 2),
            data.UsuarioClienteAId,
            RolesSistema.Cliente);

        var verAjena = await Assert.ThrowsAsync<AppException>(() =>
            service.GetByIdAsync(ajena.Id, data.UsuarioClienteAId, RolesSistema.Cliente));
        Assert.Equal(403, verAjena.StatusCode);

        var porOtro = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                Request(data.MesaCap2Id, Catorce, new TimeOnly(15, 0), personas: 1, clienteId: data.ClienteBId),
                data.UsuarioClienteAId,
                RolesSistema.Cliente));
        Assert.Equal(403, porOtro.StatusCode);

        var finalizar = await Assert.ThrowsAsync<AppException>(() =>
            service.CambiarEstadoAsync(
                propia.Id,
                new CambiarEstadoReservaRequest { Estado = nameof(EstadoReserva.Finalizada) },
                data.UsuarioClienteAId,
                RolesSistema.Cliente));
        Assert.Equal(403, finalizar.StatusCode);
        Assert.Equal(
            EstadoReserva.Confirmada,
            (await db.Reservas.FirstAsync(r => r.Id == propia.Id)).Estado);

        var cancelada = await service.CambiarEstadoAsync(
            propia.Id,
            new CambiarEstadoReservaRequest { Estado = nameof(EstadoReserva.Cancelada) },
            data.UsuarioClienteAId,
            RolesSistema.Cliente);
        Assert.Equal(nameof(EstadoReserva.Cancelada), cancelada.Estado);

        Assert.NotNull(await service.GetByIdAsync(ajena.Id, data.AdminId, RolesSistema.Admin));
        Assert.Null(await service.GetByIdAsync(9999, data.AdminId, RolesSistema.Admin));
    }

    [Fact]
    public async Task Staff_FechaObligatoria_YFiltraPorMesa()
    {
        var (service, _, data) = CreateSut();
        await CrearAsync(service, data, data.MesaCap2Id, Doce, Trece, personas: 2);
        await CrearAsync(service, data, data.MesaCap4Id, Doce, Trece);

        var sinFecha = await Assert.ThrowsAsync<AppException>(() =>
            service.ListAsync(new ReservaFilterQuery(), data.EmpleadoId, RolesSistema.Empleado));
        Assert.Equal(400, sinFecha.StatusCode);

        var delDia = await service.ListAsync(
            new ReservaFilterQuery { Fecha = Fecha },
            data.AdminId,
            RolesSistema.Admin);
        Assert.Equal(2, delDia.Count);

        var deMesa = await service.ListAsync(
            new ReservaFilterQuery { Fecha = Fecha, MesaId = data.MesaCap4Id },
            data.EmpleadoId,
            RolesSistema.Empleado);
        var unica = Assert.Single(deMesa);
        Assert.Equal(data.MesaCap4Id, unica.MesaId);
    }

    [Fact]
    public async Task Staff_ClienteInactivoOSinId_400()
    {
        var (service, _, data) = CreateSut();

        var sinId = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                Request(data.MesaCap4Id, Doce, Trece),
                data.AdminId,
                RolesSistema.Admin));
        var inactivo = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                Request(data.MesaCap4Id, Doce, Trece, clienteId: data.ClienteInactivoId),
                data.EmpleadoId,
                RolesSistema.Empleado));

        Assert.Equal(400, sinId.StatusCode);
        Assert.Equal(400, inactivo.StatusCode);
    }

    private static Task<ReservaDto> CrearAsync(
        ReservaService service,
        Seed data,
        int mesaId,
        TimeOnly inicio,
        TimeOnly fin,
        int personas = 2)
        => service.CreateAsync(
            Request(mesaId, inicio, fin, personas, data.ClienteAId),
            data.AdminId,
            RolesSistema.Admin);

    private static CreateReservaRequest Request(
        int mesaId,
        TimeOnly inicio,
        TimeOnly fin,
        int personas = 2,
        int? clienteId = null)
        => new()
        {
            MesaId = mesaId,
            Fecha = Fecha,
            HoraInicio = inicio,
            HoraFin = fin,
            Personas = personas,
            ClienteId = clienteId
        };

    private static Usuario Usuario(string nombre, string email, Rol rol) => new()
    {
        Nombre = nombre,
        Email = email,
        PasswordHash = "x",
        RolId = rol.Id,
        Estado = UsuarioEstado.Activo,
        Rol = rol
    };
}
