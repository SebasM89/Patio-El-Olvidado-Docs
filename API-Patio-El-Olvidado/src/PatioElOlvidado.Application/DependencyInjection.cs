using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Services;

namespace PatioElOlvidado.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IPedidoService, PedidoService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<ICajaService, CajaService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IEmpleadoService, EmpleadoService>();
        services.AddScoped<IReportesService, ReportesService>();
        services.AddScoped<IMesaService, MesaService>();
        services.AddScoped<IReservaService, ReservaService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IInventarioService, InventarioService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<INotificacionService, NotificacionService>();
        services.AddScoped<IPromocionService, PromocionService>();
        services.AddScoped<IHistoriaService, HistoriaService>();
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        return services;
    }
}
