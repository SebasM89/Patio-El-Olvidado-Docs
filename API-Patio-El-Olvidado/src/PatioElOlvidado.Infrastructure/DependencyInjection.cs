using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Options;
using PatioElOlvidado.Infrastructure.Email;
using PatioElOlvidado.Infrastructure.Export;
using PatioElOlvidado.Infrastructure.Persistence;
using PatioElOlvidado.Infrastructure.Repositories;
using PatioElOlvidado.Infrastructure.Security;

namespace PatioElOlvidado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IPagoRepository, PagoRepository>();
        services.AddScoped<ICajaRepository, CajaRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IFichajeRepository, FichajeRepository>();
        services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<ICajaPdfExporter, QuestPdfCajaExporter>();
        services.AddSingleton<ICajaCsvExporter, CsvHelperCajaExporter>();
        services.AddSingleton<IVentasPdfExporter, QuestPdfVentasExporter>();
        services.AddSingleton<IVentasCsvExporter, CsvHelperVentasExporter>();
        services.AddSingleton<ICajaRangoPdfExporter, QuestPdfCajaRangoExporter>();
        services.AddSingleton<ICajaRangoCsvExporter, CsvHelperCajaRangoExporter>();
        services.AddSingleton<INominaPdfExporter, QuestPdfNominaExporter>();
        services.AddSingleton<INominaCsvExporter, CsvHelperNominaExporter>();

        return services;
    }
}
