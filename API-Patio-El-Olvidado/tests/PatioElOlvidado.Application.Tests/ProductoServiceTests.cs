using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Productos;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class ProductoServiceTests
{
    private static (ProductoService Service, AppDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var service = new ProductoService(new ProductoRepository(db), new UnitOfWork(db));
        return (service, db);
    }

    [Fact]
    public async Task Create_PersistsProducto()
    {
        var (service, db) = CreateSut();

        var created = await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Pizza",
            Descripcion = "Muzzarella",
            Precio = 5000m,
            Categoria = "Platos",
            Etiquetas = "clasico, queso",
            Activo = true
        });

        Assert.True(created.Id > 0);
        Assert.Equal("Pizza", created.Nombre);
        Assert.Equal("clasico,queso", created.Etiquetas);
        Assert.Equal(1, await db.Productos.CountAsync());
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var (service, _) = CreateSut();
        var created = await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Sopa",
            Precio = 2000m,
            Categoria = "Entradas"
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductoRequest
        {
            Nombre = "Sopa del día",
            Descripcion = "Calabaza",
            Precio = 2200m,
            Categoria = "Entradas",
            Activo = true
        });

        Assert.Equal("Sopa del día", updated.Nombre);
        Assert.Equal(2200m, updated.Precio);
        Assert.Equal("Calabaza", updated.Descripcion);
    }

    [Fact]
    public async Task List_FiltersByQ_Categoria_Etiqueta()
    {
        var (service, _) = CreateSut();
        await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Empanada",
            Precio = 1000m,
            Categoria = "Entradas",
            Etiquetas = "horno,clasico"
        });
        await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Limonada",
            Precio = 1500m,
            Categoria = "Bebidas",
            Etiquetas = "fresco"
        });
        await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Milanesa",
            Precio = 8000m,
            Categoria = "Platos",
            Etiquetas = "clasico"
        });

        var byQ = await service.ListAsync(new ProductoFilterQuery { Q = "Limo" });
        Assert.Single(byQ);
        Assert.Equal("Limonada", byQ[0].Nombre);

        var byCat = await service.ListAsync(new ProductoFilterQuery { Categoria = "Entradas" });
        Assert.Single(byCat);

        var byTag = await service.ListAsync(new ProductoFilterQuery { Etiqueta = "clasico" });
        Assert.Equal(2, byTag.Count);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndHiddenFromDefaultList()
    {
        var (service, db) = CreateSut();
        var created = await service.CreateAsync(new CreateProductoRequest
        {
            Nombre = "Temporal",
            Precio = 100m,
            Categoria = "Otros"
        });

        await service.DeleteAsync(created.Id);

        var entity = await db.Productos.SingleAsync();
        Assert.False(entity.Activo);

        var listDefault = await service.ListAsync(new ProductoFilterQuery());
        Assert.Empty(listDefault);

        var listAll = await service.ListAsync(new ProductoFilterQuery { SoloActivos = false });
        Assert.Single(listAll);
        Assert.False(listAll[0].Activo);
    }

    [Fact]
    public async Task Update_NotFound_ThrowsAppException()
    {
        var (service, _) = CreateSut();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(999, new UpdateProductoRequest
            {
                Nombre = "X",
                Precio = 1,
                Categoria = "Y"
            }));

        Assert.Equal(404, ex.StatusCode);
    }
}
