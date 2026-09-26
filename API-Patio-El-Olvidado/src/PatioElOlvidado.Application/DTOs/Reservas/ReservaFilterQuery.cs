namespace PatioElOlvidado.Application.DTOs.Reservas;

public class ReservaFilterQuery
{
    public DateOnly? Fecha { get; set; }
    public int? MesaId { get; set; }
}
