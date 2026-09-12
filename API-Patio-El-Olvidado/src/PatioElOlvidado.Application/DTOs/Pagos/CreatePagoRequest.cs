namespace PatioElOlvidado.Application.DTOs.Pagos;

public class CreatePagoRequest
{
    public int PedidoId { get; set; }
    /// <summary>Efectivo | Tarjeta | Transferencia</summary>
    public string Metodo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
