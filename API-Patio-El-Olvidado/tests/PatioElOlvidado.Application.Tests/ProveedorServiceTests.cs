using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Proveedores;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class ProveedorServiceTests
{
    private static (ProveedorService Service, AppDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var service = new ProveedorService(new ProveedorRepository(db), new UnitOfWork(db));
        return (service, db);
    }

    [Fact]
    public async Task Alta_RecortaNombreYPersiste()
    {
        var (service, db) = CreateSut();

        var dto = await service.CreateAsync(new CreateProveedorRequest
        {
            Nombre = "  Molino Sur  ",
            Contacto = " Ana ",
            Telefono = " 111 ",
            Email = " ana@molino.test ",
            Notas = " Semanal ",
            Activo = true
        });

        Assert.Equal("Molino Sur", dto.Nombre);
        Assert.Equal("Ana", dto.Contacto);
        Assert.True(dto.Activo);
        Assert.Equal(1, await db.Proveedores.CountAsync());
    }

    [Fact]
    public async Task NombreDuplicado_409_CaseInsensitive()
    {
        var (service, _) = CreateSut();
        await service.CreateAsync(new CreateProveedorRequest { Nombre = "Molino Sur" });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreateProveedorRequest { Nombre = "molino sur" }));

        Assert.Equal(409, ex.StatusCode);

        var creado = (await service.ListAsync(new ProveedorFilterQuery())).Single();
        var mismo = await service.UpdateAsync(creado.Id, new UpdateProveedorRequest
        {
            Nombre = "MOLINO SUR",
            Activo = true
        });
        Assert.Equal("MOLINO SUR", mismo.Nombre);

        await service.CreateAsync(new CreateProveedorRequest { Nombre = "Otro" });
        var otro = (await service.ListAsync(new ProveedorFilterQuery { Q = "Otro" })).Single();
        var exOtro = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(otro.Id, new UpdateProveedorRequest { Nombre = "Molino Sur", Activo = true }));
        Assert.Equal(409, exOtro.StatusCode);
    }

    [Fact]
    public async Task Update_Inexistente_404_YBajaLogica()
    {
        var (service, db) = CreateSut();

        var missing = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(40, new UpdateProveedorRequest { Nombre = "Nadie", Activo = false }));
        Assert.Equal(404, missing.StatusCode);

        var creado = await service.CreateAsync(new CreateProveedorRequest { Nombre = "Verdulería" });
        var actualizado = await service.UpdateAsync(creado.Id, new UpdateProveedorRequest
        {
            Nombre = "Verdulería Norte",
            Activo = false
        });

        Assert.False(actualizado.Activo);
        Assert.False((await db.Proveedores.SingleAsync()).Activo);

        var activos = await service.ListAsync(new ProveedorFilterQuery { Activo = true });
        Assert.DoesNotContain(activos, p => p.Id == creado.Id);
    }
}
