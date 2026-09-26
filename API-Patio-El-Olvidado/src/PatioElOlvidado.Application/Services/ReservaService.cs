using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Reservas;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Constants;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservas;
    private readonly IMesaRepository _mesas;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _unitOfWork;

    public ReservaService(
        IReservaRepository reservas,
        IMesaRepository mesas,
        IClienteRepository clientes,
        IUnitOfWork unitOfWork)
    {
        _reservas = reservas;
        _mesas = mesas;
        _clientes = clientes;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservaDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var reserva = await _reservas.GetByIdAsync(id, cancellationToken);
        if (reserva is null)
            return null;

        await EnsureAccesoAsync(reserva, usuarioId, rol, cancellationToken);
        return Map(reserva);
    }

    public async Task<IReadOnlyList<ReservaDto>> ListAsync(
        ReservaFilterQuery filter,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        if (IsCliente(rol))
        {
            var cliente = await _clientes.GetByUsuarioIdAsync(usuarioId, cancellationToken);
            if (cliente is null)
                return Array.Empty<ReservaDto>();

            var propias = await _reservas.ListByClienteIdAsync(cliente.Id, cancellationToken);
            return propias.Select(Map).ToList();
        }

        if (!IsStaff(rol))
            throw new AppException("No tenés permiso para listar reservas.", StatusCodes.Status403Forbidden);

        if (filter.Fecha is null || filter.Fecha == default)
            throw new AppException("La fecha es obligatoria.", StatusCodes.Status400BadRequest);

        if (filter.MesaId is <= 0)
            throw new AppException("MesaId inválido.", StatusCodes.Status400BadRequest);

        var items = await _reservas.ListByFechaAsync(filter.Fecha.Value, filter.MesaId, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ReservaDto> CreateAsync(
        CreateReservaRequest request,
        int creadoPorUsuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        if (request.HoraFin <= request.HoraInicio)
            throw new AppException(
                "La hora de fin debe ser posterior a la hora de inicio.",
                StatusCodes.Status400BadRequest);

        if (request.Personas <= 0)
            throw new AppException(
                "La cantidad de personas debe ser mayor a cero.",
                StatusCodes.Status400BadRequest);

        var cliente = await ResolveClienteAsync(request, creadoPorUsuarioId, rol, cancellationToken);
        var mesa = await _mesas.GetByIdAsync(request.MesaId, cancellationToken)
            ?? throw new AppException("Mesa no encontrada.", StatusCodes.Status404NotFound);

        if (request.Personas > mesa.Capacidad)
            throw new AppException(
                $"La mesa {mesa.Numero} admite hasta {mesa.Capacidad} personas.",
                StatusCodes.Status400BadRequest);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existentes = await _reservas.ListByMesaYFechaAsync(mesa.Id, request.Fecha, ct);
            var solapa = existentes.Any(r =>
                r.Estado == EstadoReserva.Confirmada &&
                ReservaRules.IntervalosSeSolapan(
                    r.HoraInicio,
                    r.HoraFin,
                    request.HoraInicio,
                    request.HoraFin));

            if (solapa)
                throw new AppException(
                    $"La mesa {mesa.Numero} ya tiene una reserva confirmada que se superpone en ese horario.",
                    StatusCodes.Status409Conflict);

            var reserva = new Reserva
            {
                ClienteId = cliente.Id,
                MesaId = mesa.Id,
                Fecha = request.Fecha,
                HoraInicio = request.HoraInicio,
                HoraFin = request.HoraFin,
                Personas = request.Personas,
                Estado = EstadoReserva.Confirmada,
                CreadoPorUsuarioId = creadoPorUsuarioId,
                FechaCreacion = DateTime.UtcNow,
                Cliente = cliente,
                Mesa = mesa
            };

            await _reservas.AddAsync(reserva, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Map(reserva);
        }, cancellationToken);
    }

    public async Task<ReservaDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoReservaRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<EstadoReserva>(request.Estado, out var nuevo)
            || nuevo is not (EstadoReserva.Cancelada or EstadoReserva.Finalizada))
            throw new AppException(
                "El estado debe ser Cancelada o Finalizada.",
                StatusCodes.Status400BadRequest);

        var reserva = await _reservas.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Reserva no encontrada.", StatusCodes.Status404NotFound);

        await EnsureAccesoAsync(reserva, usuarioId, rol, cancellationToken);

        if (IsCliente(rol) && nuevo != EstadoReserva.Cancelada)
            throw new AppException("Solo podés cancelar tus reservas.", StatusCodes.Status403Forbidden);

        if (reserva.Estado != EstadoReserva.Confirmada)
            throw new AppException(
                "Solo se puede cambiar una reserva confirmada.",
                StatusCodes.Status409Conflict);

        reserva.Estado = nuevo;
        await _reservas.UpdateAsync(reserva, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(reserva);
    }

    private async Task<Cliente> ResolveClienteAsync(
        CreateReservaRequest request,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken)
    {
        if (IsCliente(rol))
        {
            var vinculado = await _clientes.GetByUsuarioIdAsync(usuarioId, cancellationToken)
                ?? throw new AppException(
                    "No hay un cliente vinculado a tu usuario.",
                    StatusCodes.Status400BadRequest);

            if (!vinculado.Activo)
                throw new AppException("El cliente vinculado está inactivo.", StatusCodes.Status400BadRequest);

            if (request.ClienteId.HasValue && request.ClienteId.Value != vinculado.Id)
                throw new AppException(
                    "No podés reservar a nombre de otro cliente.",
                    StatusCodes.Status403Forbidden);

            return vinculado;
        }

        if (!IsStaff(rol))
            throw new AppException("No tenés permiso para crear reservas.", StatusCodes.Status403Forbidden);

        if (!request.ClienteId.HasValue)
            throw new AppException("ClienteId es obligatorio.", StatusCodes.Status400BadRequest);

        var cliente = await _clientes.GetByIdAsync(request.ClienteId.Value, cancellationToken)
            ?? throw new AppException("Cliente no encontrado.", StatusCodes.Status400BadRequest);

        if (!cliente.Activo)
            throw new AppException("El cliente está inactivo.", StatusCodes.Status400BadRequest);

        return cliente;
    }

    private async Task EnsureAccesoAsync(
        Reserva reserva,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken)
    {
        if (IsStaff(rol))
            return;

        if (!IsCliente(rol))
            throw new AppException("No tenés permiso para acceder a esta reserva.", StatusCodes.Status403Forbidden);

        var cliente = await _clientes.GetByUsuarioIdAsync(usuarioId, cancellationToken);
        if (cliente is null || reserva.ClienteId != cliente.Id)
            throw new AppException("No podés acceder a una reserva ajena.", StatusCodes.Status403Forbidden);
    }

    private static ReservaDto Map(Reserva reserva) => new()
    {
        Id = reserva.Id,
        ClienteId = reserva.ClienteId,
        ClienteNombre = reserva.Cliente?.Nombre ?? string.Empty,
        MesaId = reserva.MesaId,
        MesaNumero = reserva.Mesa?.Numero ?? 0,
        Fecha = reserva.Fecha,
        HoraInicio = reserva.HoraInicio,
        HoraFin = reserva.HoraFin,
        Personas = reserva.Personas,
        Estado = reserva.Estado.ToString(),
        CreadoPorUsuarioId = reserva.CreadoPorUsuarioId,
        FechaCreacion = reserva.FechaCreacion
    };

    private static bool IsCliente(string rol)
        => string.Equals(rol, RolesSistema.Cliente, StringComparison.Ordinal);

    private static bool IsStaff(string rol)
        => string.Equals(rol, RolesSistema.Admin, StringComparison.Ordinal)
           || string.Equals(rol, RolesSistema.Empleado, StringComparison.Ordinal);

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
