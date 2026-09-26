using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Historia;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Validators;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>
/// RN-14: una sola historia (fila Id = 1). No inserta ni borra.
/// No escribe precios, pedidos ni promociones.
/// </summary>
public class HistoriaService : IHistoriaService
{
    private readonly IHistoriaRepository _historias;
    private readonly IUnitOfWork _unitOfWork;

    public HistoriaService(IHistoriaRepository historias, IUnitOfWork unitOfWork)
    {
        _historias = historias;
        _unitOfWork = unitOfWork;
    }

    public async Task<HistoriaDto?> GetAsync(string rol, CancellationToken cancellationToken = default)
    {
        EnsureConsulta(rol);

        var historia = await _historias.GetAsync(cancellationToken);
        return historia is null ? null : Map(historia);
    }

    public async Task<HistoriaDto> UpdateAsync(
        UpdateHistoriaRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(rol);

        var titulo = (request.Titulo ?? string.Empty).Trim();
        var texto = (request.Texto ?? string.Empty).Trim();
        if (titulo.Length is < 1 or > UpdateHistoriaRequestValidator.TituloMax)
            throw new AppException("El título debe tener entre 1 y 120 caracteres.", StatusCodes.Status400BadRequest);
        if (texto.Length is < 1 or > UpdateHistoriaRequestValidator.TextoMax)
            throw new AppException("El texto debe tener entre 1 y 4000 caracteres.", StatusCodes.Status400BadRequest);

        var historia = await _historias.GetAsync(cancellationToken)
            ?? throw new AppException("Historia no configurada", StatusCodes.Status404NotFound);

        historia.Titulo = titulo;
        historia.Texto = texto;
        historia.ActualizadoUtc = DateTime.UtcNow;
        historia.ActualizadoPorUsuarioId = usuarioId;

        await _historias.UpdateAsync(historia, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(historia);
    }

    private static void EnsureConsulta(string rol)
    {
        if (!IsAdmin(rol) && !IsEmpleado(rol) && !IsCliente(rol))
            throw new AppException("No tenés permiso para consultar la historia.", StatusCodes.Status403Forbidden);
    }

    private static void EnsureAdmin(string rol)
    {
        if (!IsAdmin(rol))
            throw new AppException("Solo el administrador puede editar la historia.", StatusCodes.Status403Forbidden);
    }

    private static bool IsAdmin(string rol)
        => string.Equals(rol, RolesSistema.Admin, StringComparison.Ordinal);

    private static bool IsEmpleado(string rol)
        => string.Equals(rol, RolesSistema.Empleado, StringComparison.Ordinal);

    private static bool IsCliente(string rol)
        => string.Equals(rol, RolesSistema.Cliente, StringComparison.Ordinal);

    private static HistoriaDto Map(Domain.Entities.HistoriaRestaurante historia) => new()
    {
        Id = historia.Id,
        Titulo = historia.Titulo,
        Texto = historia.Texto,
        ActualizadoUtc = historia.ActualizadoUtc,
        ActualizadoPorUsuarioId = historia.ActualizadoPorUsuarioId
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
    }
}
