using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Clientes;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Services;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;

namespace PatioElOlvidado.Application.Tests;

public class ClienteServiceTests
{
    private static (ClienteService Service, AppDbContext Db, PedidoService Pedidos, Producto Producto) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var rolCliente = new Rol { Nombre = RolesSistema.Cliente, Descripcion = "Cliente" };
        var rolAdmin = new Rol { Nombre = RolesSistema.Admin, Descripcion = "Admin" };
        db.Roles.AddRange(rolCliente, rolAdmin);
        db.SaveChanges();

        var usuarioCliente = new Usuario
        {
            Nombre = "User Cliente",
            Email = "c@test.local",
            PasswordHash = "x",
            RolId = rolCliente.Id,
            Estado = UsuarioEstado.Activo,
            Rol = rolCliente
        };
        db.Usuarios.Add(usuarioCliente);

        var producto = new Producto
        {
            Nombre = "Empanada",
            Precio = 1000m,
            Categoria = "Entradas",
            Activo = true
        };
        db.Productos.Add(producto);
        db.SaveChanges();

        var service = new ClienteService(
            new ClienteRepository(db),
            new PedidoRepository(db),
            new UsuarioRepository(db),
            new UnitOfWork(db));
        var pedidos = new PedidoService(
            new PedidoRepository(db),
            new ProductoRepository(db),
            new ClienteRepository(db),
            new UnitOfWork(db));

        return (service, db, pedidos, producto);
    }

    [Fact]
    public async Task Create_AndList_Ok()
    {
        var (service, _, _, _) = CreateSut();

        var created = await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Ana",
            Telefono = "111",
            Email = "ana@test.local",
            Activo = true
        });

        Assert.True(created.Id > 0);
        Assert.Equal(0, created.Visitas);

        var list = await service.ListAsync(new ClienteFilterQuery { Activo = true });
        Assert.Contains(list, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Delete_SoftDelete_ActivoFalse()
    {
        var (service, db, _, _) = CreateSut();
        var created = await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Baja",
            Telefono = "222",
            Activo = true
        });

        await service.DeleteAsync(created.Id);
        var entity = await db.Clientes.FirstAsync(c => c.Id == created.Id);
        Assert.False(entity.Activo);
    }

    [Fact]
    public async Task GetById_ClienteRol_SoloPropio()
    {
        var (service, db, _, _) = CreateSut();
        var usuario = await db.Usuarios.FirstAsync();
        var propio = await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Propio",
            Telefono = "333",
            UsuarioId = usuario.Id,
            Activo = true
        });
        var ajeno = await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Ajeno",
            Telefono = "444",
            Activo = true
        });

        Assert.NotNull(await service.GetByIdAsync(propio.Id, usuario.Id, RolesSistema.Cliente));
        Assert.Null(await service.GetByIdAsync(ajeno.Id, usuario.Id, RolesSistema.Cliente));
        Assert.NotNull(await service.GetByIdAsync(ajeno.Id, 1, RolesSistema.Admin));
    }

    [Fact]
    public async Task Historial_ProyectaPedidosDelCliente()
    {
        var (service, _, pedidos, producto) = CreateSut();
        var cliente = await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Hist",
            Telefono = "555",
            Activo = true
        });

        await pedidos.CreateAsync(new CreatePedidoRequest
        {
            Tipo = PedidoTipo.Local,
            ClienteId = cliente.Id,
            Detalles = [new DetallePedidoLineRequest { ProductoId = producto.Id, Cantidad = 1 }]
        }, 1);

        var historial = await service.GetHistorialAsync(cliente.Id, 1, RolesSistema.Admin);
        Assert.Single(historial);
        Assert.Equal(1000m, historial[0].Total);
    }

    [Fact]
    public async Task Create_EmailDuplicado_Throws409()
    {
        var (service, _, _, _) = CreateSut();
        await service.CreateAsync(new CreateClienteRequest
        {
            Nombre = "Uno",
            Telefono = "1",
            Email = "dup@test.local",
            Activo = true
        });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreateClienteRequest
            {
                Nombre = "Dos",
                Telefono = "2",
                Email = "dup@test.local",
                Activo = true
            }));
        Assert.Equal(409, ex.StatusCode);
    }
}
