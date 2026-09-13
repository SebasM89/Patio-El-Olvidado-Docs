namespace PatioElOlvidado.Application.DTOs.Reportes;

public class NominaReporteDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal TotalHoras { get; set; }
    public decimal TotalMonto { get; set; }
    public IReadOnlyList<NominaLineaDto> Lineas { get; set; } = Array.Empty<NominaLineaDto>();
}

public class NominaLineaDto
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
    public decimal Horas { get; set; }
    public decimal TarifaHoraSnapshot { get; set; }
    public decimal Monto { get; set; }
    public DateTime GeneradaEnUtc { get; set; }
}
