using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Domain.Entities;

/// <summary>CU08 — reserva básica. <see cref="Fecha"/> es día civil (DATE), no UTC.</summary>
public class Reserva
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int MesaId { get; set; }
    /// <summary>Día civil del restaurante. No es un instante UTC.</summary>
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int Personas { get; set; }
    public EstadoReserva Estado { get; set; }
    public int CreadoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Mesa Mesa { get; set; } = null!;
    public Usuario CreadoPorUsuario { get; set; } = null!;
}
