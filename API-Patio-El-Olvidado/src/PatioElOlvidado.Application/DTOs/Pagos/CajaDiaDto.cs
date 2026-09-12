namespace PatioElOlvidado.Application.DTOs.Pagos;

public class CajaDiaDto
{
    public DateOnly Fecha { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal Total { get; set; }
}
