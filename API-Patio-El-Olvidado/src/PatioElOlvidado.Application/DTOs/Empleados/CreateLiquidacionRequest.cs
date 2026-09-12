namespace PatioElOlvidado.Application.DTOs.Empleados;

public class CreateLiquidacionRequest
{
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
}
