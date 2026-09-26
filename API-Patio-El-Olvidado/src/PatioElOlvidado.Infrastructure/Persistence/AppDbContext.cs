using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallePedidos => Set<DetallePedido>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Fichaje> Fichajes => Set<Fichaje>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", t => t.HasCheckConstraint(
                "CK_Usuarios_Estado",
                "[Estado] IN (N'Activo', N'Inactivo', N'Bloqueado')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("UX_Usuarios_Email");
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Estado)
                .HasMaxLength(30)
                .IsRequired()
                .HasDefaultValue("Activo");
            entity.Property(x => x.UltimoAcceso).HasColumnType("datetime2");
            entity.Property(x => x.IntentosFallidos)
                .IsRequired()
                .HasDefaultValue(0);
            entity.Property(x => x.BloqueadoHasta).HasColumnType("datetime2");
            entity.HasIndex(x => x.RolId).HasDatabaseName("IX_Usuarios_RolId");
            entity.HasIndex(x => x.Estado).HasDatabaseName("IX_Usuarios_Estado");
            entity.HasOne(x => x.Rol)
                .WithMany(x => x.Usuarios)
                .HasForeignKey(x => x.RolId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
            entity.Property(x => x.Descripcion).HasMaxLength(200);
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.ToTable("Permisos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(200);
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.ToTable("RolPermisos");
            entity.HasKey(x => new { x.RolId, x.PermisoId });
            entity.HasOne(x => x.Rol)
                .WithMany(x => x.RolPermisos)
                .HasForeignKey(x => x.RolId);
            entity.HasOne(x => x.Permiso)
                .WithMany(x => x.RolPermisos)
                .HasForeignKey(x => x.PermisoId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.ReplacedByToken).HasMaxLength(500);
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.PasswordResetTokens)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(500);
            entity.Property(x => x.Precio).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Imagen).HasMaxLength(500);
            entity.Property(x => x.Etiquetas).HasMaxLength(300);
            entity.Property(x => x.Activo).IsRequired();
            entity.HasIndex(x => x.Categoria);
            entity.HasIndex(x => x.Nombre);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Telefono).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150);
            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");
            entity.Property(x => x.Visitas).IsRequired();
            entity.Property(x => x.Activo).IsRequired();
            entity.HasIndex(x => x.Telefono);
            entity.HasIndex(x => x.UsuarioId)
                .IsUnique()
                .HasFilter("[UsuarioId] IS NOT NULL");
            entity.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.ToTable("Empleados");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Puesto).HasMaxLength(100);
            entity.Property(x => x.Telefono).HasMaxLength(30);
            entity.Property(x => x.TarifaHora).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.HorasTrabajadas).HasPrecision(10, 4).IsRequired();
            entity.Property(x => x.Activo).IsRequired();
            entity.HasIndex(x => x.Nombre);
            entity.HasIndex(x => x.UsuarioId)
                .IsUnique()
                .HasFilter("[UsuarioId] IS NOT NULL");
            entity.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Fichaje>(entity =>
        {
            entity.ToTable("Fichajes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntradaUtc).IsRequired();
            entity.Property(x => x.Horas).HasPrecision(10, 4);
            entity.HasIndex(x => x.EmpleadoId);
            entity.HasIndex(x => x.EntradaUtc);
            entity.HasOne(x => x.Empleado)
                .WithMany(e => e.Fichajes)
                .HasForeignKey(x => x.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Liquidacion>(entity =>
        {
            entity.ToTable("Liquidaciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PeriodoDesde).IsRequired();
            entity.Property(x => x.PeriodoHasta).IsRequired();
            entity.Property(x => x.Horas).HasPrecision(10, 4).IsRequired();
            entity.Property(x => x.TarifaHoraSnapshot).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Monto).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.GeneradaEnUtc).IsRequired();
            entity.HasIndex(x => x.EmpleadoId);
            entity.HasOne(x => x.Empleado)
                .WithMany(e => e.Liquidaciones)
                .HasForeignKey(x => x.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GeneradaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.GeneradaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Subtotal).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Total).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.VisitaContabilizada).IsRequired();
            entity.Property(x => x.FechaCreacion).IsRequired();
            entity.HasIndex(x => x.Estado);
            entity.HasIndex(x => x.FechaCreacion);
            entity.HasIndex(x => x.ClienteId);
            entity.HasOne(x => x.CreadoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.ToTable("DetallePedidos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Cantidad).IsRequired();
            entity.Property(x => x.PrecioUnitario).HasPrecision(10, 2).IsRequired();
            entity.HasOne(x => x.Pedido)
                .WithMany(x => x.Detalles)
                .HasForeignKey(x => x.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Caja>(entity =>
        {
            entity.ToTable("Caja");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Fecha).IsRequired();
            entity.HasIndex(x => x.Fecha).IsUnique();
            entity.Property(x => x.TotalEfectivo).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.TotalTarjeta).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.TotalTransferencia).HasPrecision(10, 2).IsRequired();
            entity.Ignore(x => x.Total);
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pagos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Metodo).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Monto).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.FechaPago).IsRequired();
            entity.HasIndex(x => x.PedidoId);
            entity.HasOne(x => x.Pedido)
                .WithMany(x => x.Pagos)
                .HasForeignKey(x => x.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Caja)
                .WithMany(x => x.Pagos)
                .HasForeignKey(x => x.CajaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.ToTable("Mesas", t => t.HasCheckConstraint("CK_Mesas_Capacidad", "[Capacidad] > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Numero).IsRequired();
            entity.Property(x => x.Capacidad).IsRequired();
            entity.Property(x => x.Ubicacion).HasMaxLength(100);
            entity.HasIndex(x => x.Numero)
                .IsUnique()
                .HasDatabaseName("IX_Mesas_Numero");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reservas", t =>
            {
                t.HasCheckConstraint("CK_Reservas_HoraFin", "[HoraFin] > [HoraInicio]");
                t.HasCheckConstraint("CK_Reservas_Personas", "[Personas] > 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Fecha).HasColumnType("date").IsRequired();
            entity.Property(x => x.HoraInicio).HasColumnType("time").IsRequired();
            entity.Property(x => x.HoraFin).HasColumnType("time").IsRequired();
            entity.Property(x => x.Personas).IsRequired();
            entity.Property(x => x.Estado)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(x => x.FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd()
                .IsRequired();
            entity.HasIndex(x => new { x.MesaId, x.Fecha })
                .HasDatabaseName("IX_Reservas_MesaId_Fecha");
            entity.HasIndex(x => x.ClienteId)
                .HasDatabaseName("IX_Reservas_ClienteId");
            entity.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reservas_Clientes");
            entity.HasOne(x => x.Mesa)
                .WithMany(x => x.Reservas)
                .HasForeignKey(x => x.MesaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reservas_Mesas");
            entity.HasOne(x => x.CreadoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Reservas_Usuarios");
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("StockItems", t =>
            {
                t.HasCheckConstraint(
                    "CK_StockItems_Unidad",
                    "[Unidad] IN (N'Unidad', N'Kg', N'L')");
                t.HasCheckConstraint("CK_StockItems_CantidadActual", "[CantidadActual] >= 0");
                t.HasCheckConstraint("CK_StockItems_StockMinimo", "[StockMinimo] >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Descripcion).HasMaxLength(300);
            entity.Property(x => x.Unidad)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
            entity.Property(x => x.CantidadActual)
                .HasPrecision(12, 3)
                .HasDefaultValue(0m)
                .IsRequired();
            entity.Property(x => x.StockMinimo)
                .HasPrecision(12, 3)
                .HasDefaultValue(0m)
                .IsRequired();
            entity.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_StockItems_Nombre");
            entity.HasIndex(x => x.Activo)
                .HasDatabaseName("IX_StockItems_Activo");
        });

        modelBuilder.Entity<MovimientoStock>(entity =>
        {
            entity.ToTable("MovimientosStock", t =>
            {
                t.HasCheckConstraint(
                    "CK_MovimientosStock_Tipo",
                    "[Tipo] IN (N'Entrada', N'Salida')");
                t.HasCheckConstraint("CK_MovimientosStock_Cantidad", "[Cantidad] > 0");
                t.HasCheckConstraint(
                    "CK_MovimientosStock_ProveedorSoloEntrada",
                    "[ProveedorId] IS NULL OR [Tipo] = N'Entrada'");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
            entity.Property(x => x.Cantidad)
                .HasPrecision(12, 3)
                .IsRequired();
            entity.Property(x => x.Motivo).HasMaxLength(200);
            entity.Property(x => x.FechaUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd()
                .IsRequired();
            entity.HasIndex(x => new { x.StockItemId, x.FechaUtc })
                .IsDescending(false, true)
                .HasDatabaseName("IX_MovimientosStock_StockItemId_FechaUtc");
            entity.HasIndex(x => x.ProveedorId)
                .HasFilter("[ProveedorId] IS NOT NULL")
                .HasDatabaseName("IX_MovimientosStock_ProveedorId");
            entity.HasOne(x => x.StockItem)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.StockItemId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_MovimientosStock_StockItems");
            entity.HasOne(x => x.RegistradoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.RegistradoPorUsuarioId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_MovimientosStock_Usuarios");
            entity.HasOne(x => x.Proveedor)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.ProveedorId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_MovimientosStock_Proveedores");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("Proveedores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Contacto).HasMaxLength(100);
            entity.Property(x => x.Telefono).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(150);
            entity.Property(x => x.Notas).HasMaxLength(300);
            entity.Property(x => x.Activo)
                .IsRequired()
                .HasDefaultValue(true);
            entity.HasIndex(x => x.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_Proveedores_Nombre");
            entity.HasIndex(x => x.Activo)
                .HasDatabaseName("IX_Proveedores_Activo");
        });
    }
}
