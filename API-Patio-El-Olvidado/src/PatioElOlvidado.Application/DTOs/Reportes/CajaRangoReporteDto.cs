using PatioElOlvidado.Application.DTOs.Pagos;

namespace PatioElOlvidado.Application.DTOs.Reportes;

public class CajaRangoReporteDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<CajaDiaDto> Dias { get; set; } = Array.Empty<CajaDiaDto>();
}
