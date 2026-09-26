using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Historia;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class HistoriaServiceTests
{
    private const string SemillaTitulo = "Patio El Olvidado";
    private const string SemillaTexto =
        "Patio El Olvidado es el restaurante que este sistema administra. El administrador puede reemplazar este texto.";

    private static (HistoriaService Service, AppDbContext Db) CreateSut(bool seed)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        if (seed)
        {
            db.HistoriaRestaurante.Add(new HistoriaRestaurante
            {
                Id = 1,
                Titulo = SemillaTitulo,
                Texto = SemillaTexto
            });
            db.SaveChanges();
        }

        var service = new HistoriaService(new HistoriaRepository(db), new UnitOfWork(db));
        return (service, db);
    }

    private static async Task<HistoriaRestaurante?> LeerFila(AppDbContext db)
    {
        db.ChangeTracker.Clear();
        return await db.HistoriaRestaurante.SingleOrDefaultAsync();
    }

    [Theory]
    [InlineData(RolesSistema.Admin)]
    [InlineData(RolesSistema.Empleado)]
    [InlineData(RolesSistema.Cliente)]
    public async Task Get_TresRoles_VenLaSemilla(string rol)
    {
        var (service, _) = CreateSut(seed: true);

        var dto = await service.GetAsync(rol);

        Assert.NotNull(dto);
        Assert.Equal(1, dto!.Id);
        Assert.Equal(SemillaTitulo, dto.Titulo);
        Assert.Equal(SemillaTexto, dto.Texto);
        Assert.Null(dto.ActualizadoUtc);
        Assert.Null(dto.ActualizadoPorUsuarioId);
    }

    [Fact]
    public async Task Put_Admin_PersisteTituloTextoUtcYUsuario_YLosTresRolesLaVen()
    {
        var (service, db) = CreateSut(seed: true);
        var antes = DateTime.UtcNow.AddSeconds(-2);

        var dto = await service.UpdateAsync(
            new UpdateHistoriaRequest { Titulo = "  Carta nueva  ", Texto = "  El patio cambió de texto.  " },
            usuarioId: 15,
            RolesSistema.Admin);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Carta nueva", dto.Titulo);
        Assert.Equal("El patio cambió de texto.", dto.Texto);
        Assert.NotNull(dto.ActualizadoUtc);
        Assert.InRange(dto.ActualizadoUtc!.Value, antes, DateTime.UtcNow.AddSeconds(2));
        Assert.Equal(15, dto.ActualizadoPorUsuarioId);

        var fila = await LeerFila(db);
        Assert.NotNull(fila);
        Assert.Equal(1, fila!.Id);
        Assert.Equal("Carta nueva", fila.Titulo);
        Assert.Equal("El patio cambió de texto.", fila.Texto);
        Assert.NotNull(fila.ActualizadoUtc);
        Assert.Equal(15, fila.ActualizadoPorUsuarioId);
        Assert.Equal(1, await db.HistoriaRestaurante.CountAsync());

        foreach (var rol in new[] { RolesSistema.Admin, RolesSistema.Empleado, RolesSistema.Cliente })
        {
            var vista = await service.GetAsync(rol);
            Assert.Equal("Carta nueva", vista!.Titulo);
            Assert.Equal("El patio cambió de texto.", vista.Texto);
            Assert.Equal(15, vista.ActualizadoPorUsuarioId);
        }
    }

    [Theory]
    [InlineData(RolesSistema.Empleado)]
    [InlineData(RolesSistema.Cliente)]
    public async Task Put_EmpleadoYCliente_403_NoCambiaLaFila(string rol)
    {
        var (service, db) = CreateSut(seed: true);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(
            new UpdateHistoriaRequest { Titulo = "Intento", Texto = "No debe guardarse" },
            usuarioId: 8,
            rol));

        Assert.Equal(403, ex.StatusCode);
        var fila = await LeerFila(db);
        Assert.Equal(SemillaTitulo, fila!.Titulo);
        Assert.Equal(SemillaTexto, fila.Texto);
        Assert.Null(fila.ActualizadoUtc);
        Assert.Null(fila.ActualizadoPorUsuarioId);
    }

    [Theory]
    [MemberData(nameof(PayloadsInvalidos))]
    public async Task Put_Validacion_400_NoCambiaLaFila(string titulo, string texto)
    {
        var (service, db) = CreateSut(seed: true);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(
            new UpdateHistoriaRequest { Titulo = titulo, Texto = texto },
            usuarioId: 3,
            RolesSistema.Admin));

        Assert.Equal(400, ex.StatusCode);
        var fila = await LeerFila(db);
        Assert.Equal(SemillaTitulo, fila!.Titulo);
        Assert.Equal(SemillaTexto, fila.Texto);
        Assert.Null(fila.ActualizadoUtc);
        Assert.Null(fila.ActualizadoPorUsuarioId);
    }

    [Fact]
    public async Task SinFila_GetNull_Put404_NoInserta()
    {
        var (service, db) = CreateSut(seed: false);

        Assert.Null(await service.GetAsync(RolesSistema.Admin));
        Assert.Null(await service.GetAsync(RolesSistema.Empleado));
        Assert.Null(await service.GetAsync(RolesSistema.Cliente));

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(
            new UpdateHistoriaRequest { Titulo = "Alta", Texto = "No corresponde" },
            usuarioId: 1,
            RolesSistema.Admin));

        Assert.Equal(404, ex.StatusCode);
        Assert.Null(await LeerFila(db));
    }

    public static IEnumerable<object[]> PayloadsInvalidos()
    {
        yield return new object[] { "", "Texto válido" };
        yield return new object[] { new string('a', 121), "Texto válido" };
        yield return new object[] { "Título válido", "" };
        yield return new object[] { "Título válido", new string('b', 4001) };
        yield return new object[] { "   ", "Texto válido" };
        yield return new object[] { "Título válido", "   " };
    }
}
