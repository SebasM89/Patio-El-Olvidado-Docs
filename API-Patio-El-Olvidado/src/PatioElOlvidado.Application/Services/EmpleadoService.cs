using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Empleados;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Constants;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>RF-06 / CU06 — Empleados, fichaje (RN-07) y liquidaciones MVP.</summary>
public class EmpleadoService : IEmpleadoService
{
    private readonly IEmpleadoRepository _empleados;
    private readonly IFichajeRepository _fichajes;
    private readonly ILiquidacionRepository _liquidaciones;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;

    public EmpleadoService(
        IEmpleadoRepository empleados,
        IFichajeRepository fichajes,
        ILiquidacionRepository liquidaciones,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork)
    {
        _empleados = empleados;
        _fichajes = fichajes;
        _liquidaciones = liquidaciones;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmpleadoDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.GetByIdAsync(id, cancellationToken);
        if (empleado is null)
            return null;

        if (IsEmpleado(rol) && empleado.UsuarioId != usuarioId)
            return null;

        return Map(empleado);
    }

    public async Task<EmpleadoDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.GetByUsuarioIdAsync(usuarioId, cancellationToken);
        return empleado is null ? null : Map(empleado);
    }

    public async Task<IReadOnlyList<EmpleadoDto>> ListAsync(
        EmpleadoFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _empleados.SearchAsync(filter, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<EmpleadoDto> CreateAsync(
        CreateEmpleadoRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUsuarioLinkAsync(request.UsuarioId, excludeId: null, cancellationToken);

        var empleado = new Empleado
        {
            Nombre = request.Nombre.Trim(),
            Puesto = NormalizeOptional(request.Puesto),
            Telefono = NormalizeOptional(request.Telefono),
            TarifaHora = decimal.Round(request.TarifaHora, 2, MidpointRounding.AwayFromZero),
            HorasTrabajadas = 0m,
            UsuarioId = request.UsuarioId,
            Activo = request.Activo
        };

        await _empleados.AddAsync(empleado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(empleado);
    }

    public async Task<EmpleadoDto> UpdateAsync(
        int id,
        UpdateEmpleadoRequest request,
        CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Empleado no encontrado.", StatusCodes.Status404NotFound);

        await EnsureUsuarioLinkAsync(request.UsuarioId, excludeId: id, cancellationToken);

        empleado.Nombre = request.Nombre.Trim();
        empleado.Puesto = NormalizeOptional(request.Puesto);
        empleado.Telefono = NormalizeOptional(request.Telefono);
        empleado.TarifaHora = decimal.Round(request.TarifaHora, 2, MidpointRounding.AwayFromZero);
        empleado.UsuarioId = request.UsuarioId;
        empleado.Activo = request.Activo;

        await _empleados.UpdateAsync(empleado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(empleado);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Empleado no encontrado.", StatusCodes.Status404NotFound);

        if (!empleado.Activo)
            return;

        empleado.Activo = false;
        await _empleados.UpdateAsync(empleado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FichajeDto>> ListFichajesAsync(
        int empleadoId,
        CancellationToken cancellationToken = default)
    {
        _ = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        var items = await _fichajes.ListByEmpleadoIdAsync(empleadoId, cancellationToken);
        return items.Select(MapFichaje).ToList();
    }

    public async Task<IReadOnlyList<FichajeDto>> ListMeFichajesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var empleado = await RequirePerfilMeAsync(usuarioId, cancellationToken);
        var items = await _fichajes.ListByEmpleadoIdAsync(empleado.Id, cancellationToken);
        return items.Select(MapFichaje).ToList();
    }

    public Task<FichajeDto> RegistrarEntradaAsync(int empleadoId, CancellationToken cancellationToken = default)
        => AbrirFichajeAsync(empleadoId, cancellationToken);

    public async Task<FichajeDto> RegistrarEntradaMeAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var empleado = await RequirePerfilMeAsync(usuarioId, cancellationToken);
        return await AbrirFichajeAsync(empleado.Id, cancellationToken);
    }

    public Task<FichajeDto> RegistrarSalidaAsync(int empleadoId, CancellationToken cancellationToken = default)
        => CerrarFichajeAsync(empleadoId, cancellationToken);

    public async Task<FichajeDto> RegistrarSalidaMeAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var empleado = await RequirePerfilMeAsync(usuarioId, cancellationToken);
        return await CerrarFichajeAsync(empleado.Id, cancellationToken);
    }

    public async Task<LiquidacionPreviewDto> PreviewLiquidacionAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        EnsurePeriodo(desde, hasta);
        var empleado = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        return await BuildPreviewAsync(empleado, desde, hasta, cancellationToken);
    }

    public async Task<LiquidacionPreviewDto> PreviewMeLiquidacionAsync(
        int usuarioId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        EnsurePeriodo(desde, hasta);
        var empleado = await RequirePerfilMeAsync(usuarioId, cancellationToken);
        return await BuildPreviewAsync(empleado, desde, hasta, cancellationToken);
    }

    public async Task<IReadOnlyList<LiquidacionDto>> ListLiquidacionesAsync(
        int empleadoId,
        CancellationToken cancellationToken = default)
    {
        _ = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        var items = await _liquidaciones.ListByEmpleadoIdAsync(empleadoId, cancellationToken);
        return items.Select(MapLiquidacion).ToList();
    }

    public async Task<IReadOnlyList<LiquidacionDto>> ListMeLiquidacionesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var empleado = await RequirePerfilMeAsync(usuarioId, cancellationToken);
        var items = await _liquidaciones.ListByEmpleadoIdAsync(empleado.Id, cancellationToken);
        return items.Select(MapLiquidacion).ToList();
    }

    public async Task<LiquidacionDto> GenerarLiquidacionAsync(
        int empleadoId,
        CreateLiquidacionRequest request,
        int generadaPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        EnsurePeriodo(request.PeriodoDesde, request.PeriodoHasta);
        var empleado = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        var preview = await BuildPreviewAsync(
            empleado,
            request.PeriodoDesde,
            request.PeriodoHasta,
            cancellationToken);

        if (preview.Horas <= 0)
            throw new AppException(
                "No hay fichajes cerrados en el período indicado.",
                StatusCodes.Status400BadRequest);

        var liquidacion = new Liquidacion
        {
            EmpleadoId = empleado.Id,
            PeriodoDesde = request.PeriodoDesde,
            PeriodoHasta = request.PeriodoHasta,
            Horas = preview.Horas,
            TarifaHoraSnapshot = preview.TarifaHora,
            Monto = preview.Monto,
            GeneradaEnUtc = DateTime.UtcNow,
            GeneradaPorUsuarioId = generadaPorUsuarioId
        };

        await _liquidaciones.AddAsync(liquidacion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapLiquidacion(liquidacion);
    }

    private async Task<FichajeDto> AbrirFichajeAsync(int empleadoId, CancellationToken cancellationToken)
    {
        var empleado = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        if (!empleado.Activo)
            throw new AppException("El empleado está inactivo.", StatusCodes.Status400BadRequest);

        var abierto = await _fichajes.GetAbiertoByEmpleadoIdAsync(empleadoId, cancellationToken);
        if (abierto is not null)
            throw new AppException(
                "Ya existe un fichaje abierto para este empleado.",
                StatusCodes.Status409Conflict);

        var fichaje = new Fichaje
        {
            EmpleadoId = empleadoId,
            EntradaUtc = DateTime.UtcNow
        };

        await _fichajes.AddAsync(fichaje, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapFichaje(fichaje);
    }

    private async Task<FichajeDto> CerrarFichajeAsync(int empleadoId, CancellationToken cancellationToken)
    {
        var empleado = await RequireEmpleadoAsync(empleadoId, cancellationToken);
        var abierto = await _fichajes.GetAbiertoByEmpleadoIdAsync(empleadoId, cancellationToken)
            ?? throw new AppException(
                "No hay un fichaje abierto para cerrar.",
                StatusCodes.Status409Conflict);

        var salida = DateTime.UtcNow;
        var horas = FichajeRules.CalcularHoras(abierto.EntradaUtc, salida);
        abierto.SalidaUtc = salida;
        abierto.Horas = horas;

        empleado.HorasTrabajadas = decimal.Round(
            empleado.HorasTrabajadas + horas,
            4,
            MidpointRounding.AwayFromZero);

        await _fichajes.UpdateAsync(abierto, cancellationToken);
        await _empleados.UpdateAsync(empleado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapFichaje(abierto);
    }

    private async Task<LiquidacionPreviewDto> BuildPreviewAsync(
        Empleado empleado,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var cerrados = await _fichajes.ListCerradosEnPeriodoAsync(empleado.Id, desde, hasta, cancellationToken);
        var horas = decimal.Round(
            cerrados.Sum(f => f.Horas ?? 0m),
            4,
            MidpointRounding.AwayFromZero);
        var tarifa = empleado.TarifaHora;

        return new LiquidacionPreviewDto
        {
            EmpleadoId = empleado.Id,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            Horas = horas,
            TarifaHora = tarifa,
            Monto = FichajeRules.CalcularMonto(horas, tarifa)
        };
    }

    private async Task<Empleado> RequireEmpleadoAsync(int empleadoId, CancellationToken cancellationToken)
        => await _empleados.GetByIdAsync(empleadoId, cancellationToken)
            ?? throw new AppException("Empleado no encontrado.", StatusCodes.Status404NotFound);

    private async Task<Empleado> RequirePerfilMeAsync(int usuarioId, CancellationToken cancellationToken)
        => await _empleados.GetByUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new AppException(
                "No hay perfil de empleado vinculado a este usuario.",
                StatusCodes.Status404NotFound);

    private async Task EnsureUsuarioLinkAsync(
        int? usuarioId,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (!usuarioId.HasValue)
            return;

        var usuario = await _usuarios.GetByIdAsync(usuarioId.Value, cancellationToken)
            ?? throw new AppException("Usuario no encontrado.", StatusCodes.Status400BadRequest);

        if (!string.Equals(usuario.Rol?.Nombre, RolesSistema.Empleado, StringComparison.Ordinal))
            throw new AppException(
                "Solo se puede vincular un usuario con rol Empleado.",
                StatusCodes.Status400BadRequest);

        if (usuario.Estado != UsuarioEstado.Activo)
            throw new AppException("El usuario vinculado no está activo.", StatusCodes.Status400BadRequest);

        if (await _empleados.UsuarioIdExistsAsync(usuarioId.Value, excludeId, cancellationToken))
            throw new AppException(
                "Ese usuario ya está vinculado a otro empleado.",
                StatusCodes.Status409Conflict);
    }

    private static void EnsurePeriodo(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            throw new AppException(
                "PeriodoHasta debe ser mayor o igual a PeriodoDesde.",
                StatusCodes.Status400BadRequest);
    }

    private static EmpleadoDto Map(Empleado e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Puesto = e.Puesto,
        Telefono = e.Telefono,
        TarifaHora = e.TarifaHora,
        HorasTrabajadas = e.HorasTrabajadas,
        UsuarioId = e.UsuarioId,
        Activo = e.Activo
    };

    private static FichajeDto MapFichaje(Fichaje f) => new()
    {
        Id = f.Id,
        EmpleadoId = f.EmpleadoId,
        EntradaUtc = f.EntradaUtc,
        SalidaUtc = f.SalidaUtc,
        Horas = f.Horas
    };

    private static LiquidacionDto MapLiquidacion(Liquidacion l) => new()
    {
        Id = l.Id,
        EmpleadoId = l.EmpleadoId,
        PeriodoDesde = l.PeriodoDesde,
        PeriodoHasta = l.PeriodoHasta,
        Horas = l.Horas,
        TarifaHoraSnapshot = l.TarifaHoraSnapshot,
        Monto = l.Monto,
        GeneradaEnUtc = l.GeneradaEnUtc,
        GeneradaPorUsuarioId = l.GeneradaPorUsuarioId
    };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsEmpleado(string rol)
        => string.Equals(rol, RolesSistema.Empleado, StringComparison.Ordinal);

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
