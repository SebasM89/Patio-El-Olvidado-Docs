namespace PatioElOlvidado.Application.DTOs.Empleados;

public class LiquidacionDto
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
    public decimal Horas { get; set; }
    public decimal TarifaHoraSnapshot { get; set; }
    public decimal Monto { get; set; }
    public DateTime GeneradaEnUtc { get; set; }
    public int GeneradaPorUsuarioId { get; set; }
}
