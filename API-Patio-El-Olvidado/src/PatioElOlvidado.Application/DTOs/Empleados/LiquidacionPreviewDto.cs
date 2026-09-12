namespace PatioElOlvidado.Application.DTOs.Empleados;

public class LiquidacionPreviewDto
{
    public int EmpleadoId { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
    public decimal Horas { get; set; }
    public decimal TarifaHora { get; set; }
    public decimal Monto { get; set; }
}
