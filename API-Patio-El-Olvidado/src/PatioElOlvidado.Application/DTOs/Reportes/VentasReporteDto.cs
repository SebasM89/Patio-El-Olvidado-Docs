namespace PatioElOlvidado.Application.DTOs.Reportes;

public class VentasReporteDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal Total { get; set; }
    public int CantidadPagos { get; set; }
    public int CantidadPedidos { get; set; }
    public IReadOnlyList<VentasDiaDto> Dias { get; set; } = Array.Empty<VentasDiaDto>();
}

public class VentasDiaDto
{
    public DateOnly Fecha { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal Total { get; set; }
    public int CantidadPagos { get; set; }
}
