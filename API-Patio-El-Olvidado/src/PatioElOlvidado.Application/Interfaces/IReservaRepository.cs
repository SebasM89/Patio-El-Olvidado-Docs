using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Interfaces;

/// <summary>Persistencia de Reservas. El solapamiento (RN-06) no vive acá.</summary>
public interface IReservaRepository
{
    Task<Reserva?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Filas del índice (MesaId, Fecha). Sin filtrar por estado.</summary>
    Task<IReadOnlyList<Reserva>> ListByMesaYFechaAsync(
        int mesaId,
        DateOnly fecha,
        CancellationToken cancellationToken = default);
    /// <summary>Agenda del día. <paramref name="mesaId"/> opcional. Sin filtrar por estado.</summary>
    Task<IReadOnlyList<Reserva>> ListByFechaAsync(
        DateOnly fecha,
        int? mesaId,
        CancellationToken cancellationToken = default);
    /// <summary>Reservas del cliente, cualquier fecha y estado.</summary>
    Task<IReadOnlyList<Reserva>> ListByClienteIdAsync(
        int clienteId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reserva reserva, CancellationToken cancellationToken = default);
}
