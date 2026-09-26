namespace PatioElOlvidado.Application.DTOs.Reservas;

public class CreateReservaRequest
{
    public int MesaId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int Personas { get; set; }
    /// <summary>Obligatorio para Admin y Empleado. El Cliente reserva contra su ficha vinculada.</summary>
    public int? ClienteId { get; set; }
}
