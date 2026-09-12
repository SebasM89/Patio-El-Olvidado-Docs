namespace PatioElOlvidado.Application.DTOs.Empleados;

public class FichajeDto
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public DateTime EntradaUtc { get; set; }
    public DateTime? SalidaUtc { get; set; }
    public decimal? Horas { get; set; }
    public bool Abierto => SalidaUtc is null;
}
