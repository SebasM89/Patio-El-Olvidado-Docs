using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class NotificacionServiceTests
{
    private static (NotificacionService Service, AppDbContext Db, Usuario Dueno, Usuario Otro) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var rol = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        db.Roles.Add(rol);
        db.SaveChanges();

        var dueno = new Usuario
        {
            Nombre = "Ana",
            Email = "ana@test.local",
            PasswordHash = "hash",
            RolId = rol.Id,
            Estado = UsuarioEstado.Activo
        };
        var otro = new Usuario
        {
            Nombre = "Beto",
            Email = "beto@test.local",
            PasswordHash = "hash",
            RolId = rol.Id,
            Estado = UsuarioEstado.Activo
        };
        db.Usuarios.AddRange(dueno, otro);
        db.SaveChanges();

        var service = new NotificacionService(new NotificacionRepository(db), new UnitOfWork(db));
        return (service, db, dueno, otro);
    }

    [Fact]
    public async Task List_SoloPropias_FechaUtcDesc_YFiltraLeida()
    {
        var (service, db, dueno, otro) = CreateSut();
        var vieja = Aviso(dueno.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), leida: false, movimientoId: 1);
        var nueva = Aviso(dueno.Id, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), leida: true, movimientoId: 2);
        var ajena = Aviso(otro.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), leida: false, movimientoId: 3);
        db.Notificaciones.AddRange(vieja, nueva, ajena);
        await db.SaveChangesAsync();

        var todas = await service.ListAsync(dueno.Id, null);
        Assert.Equal(2, todas.Count);
        Assert.Equal(nueva.Id, todas[0].Id);
        Assert.Equal(vieja.Id, todas[1].Id);
        Assert.Equal("StockAlerta", todas[0].Tipo);
        Assert.DoesNotContain(todas, n => n.Id == ajena.Id);

        var noLeidas = await service.ListAsync(dueno.Id, false);
        Assert.Single(noLeidas);
        Assert.Equal(vieja.Id, noLeidas[0].Id);

        var leidas = await service.ListAsync(dueno.Id, true);
        Assert.Single(leidas);
        Assert.True(leidas[0].Leida);
    }

    [Fact]
    public async Task Conteo_SoloNoLeidasPropias()
    {
        var (service, db, dueno, otro) = CreateSut();
        db.Notificaciones.AddRange(
            Aviso(dueno.Id, DateTime.UtcNow, leida: false, movimientoId: 1),
            Aviso(dueno.Id, DateTime.UtcNow, leida: false, movimientoId: 2),
            Aviso(dueno.Id, DateTime.UtcNow, leida: true, movimientoId: 3),
            Aviso(otro.Id, DateTime.UtcNow, leida: false, movimientoId: 4));
        await db.SaveChangesAsync();

        var conteo = await service.CountNoLeidasAsync(dueno.Id);
        Assert.Equal(2, conteo.Cantidad);
    }

    [Fact]
    public async Task MarcarLeida_Propia_Idempotente_AjenaOInexistente404()
    {
        var (service, db, dueno, otro) = CreateSut();
        var propia = Aviso(dueno.Id, DateTime.UtcNow, leida: false, movimientoId: 1);
        var ajena = Aviso(otro.Id, DateTime.UtcNow, leida: false, movimientoId: 2);
        db.Notificaciones.AddRange(propia, ajena);
        await db.SaveChangesAsync();

        var marcada = await service.MarcarLeidaAsync(propia.Id, dueno.Id);
        Assert.True(marcada.Leida);
        Assert.NotNull(marcada.LeidaUtc);

        var leidaUtc = marcada.LeidaUtc;
        var otraVez = await service.MarcarLeidaAsync(propia.Id, dueno.Id);
        Assert.True(otraVez.Leida);
        Assert.Equal(leidaUtc, otraVez.LeidaUtc);

        var exAjena = await Assert.ThrowsAsync<AppException>(() => service.MarcarLeidaAsync(ajena.Id, dueno.Id));
        var exMissing = await Assert.ThrowsAsync<AppException>(() => service.MarcarLeidaAsync(9999, dueno.Id));
        Assert.Equal(404, exAjena.StatusCode);
        Assert.Equal(404, exMissing.StatusCode);

        var ajenaStored = await db.Notificaciones.SingleAsync(n => n.Id == ajena.Id);
        Assert.False(ajenaStored.Leida);
    }

    private static Notificacion Aviso(int usuarioId, DateTime fechaUtc, bool leida, int movimientoId) => new()
    {
        UsuarioId = usuarioId,
        Titulo = "Stock en alerta",
        Mensaje = "Harina quedó con saldo 1 Kg (mínimo 2).",
        Tipo = TipoNotificacion.StockAlerta,
        Leida = leida,
        LeidaUtc = leida ? fechaUtc : null,
        FechaUtc = fechaUtc,
        StockItemId = 1,
        MovimientoStockId = movimientoId
    };
}
