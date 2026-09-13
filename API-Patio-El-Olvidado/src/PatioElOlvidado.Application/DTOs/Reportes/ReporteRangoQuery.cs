namespace PatioElOlvidado.Application.DTOs.Reportes;

/// <summary>Rango de fechas UTC (DateOnly) para reportes CU11.</summary>
public class ReporteRangoQuery
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
}
