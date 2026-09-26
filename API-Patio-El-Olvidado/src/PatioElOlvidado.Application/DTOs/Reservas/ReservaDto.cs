namespace PatioElOlvidado.Application.DTOs.Reservas;

public class ReservaDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int MesaId { get; set; }
    public int MesaNumero { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int Personas { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CreadoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }
}
