namespace PatioElOlvidado.Domain.Entities;

/// <summary>Cobro parcial o total de un pedido (RF-04 / CU04).</summary>
public class Pago
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    /// <summary>Efectivo | Tarjeta | Transferencia</summary>
    public string Metodo { get; set; } = string.Empty;
    /// <summary>Completado | Anulado</summary>
    public string Estado { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public int CajaId { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Caja Caja { get; set; } = null!;
}
