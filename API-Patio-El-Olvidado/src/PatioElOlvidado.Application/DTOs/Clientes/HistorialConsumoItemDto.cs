namespace PatioElOlvidado.Application.DTOs.Clientes;

/// <summary>CU09 — proyección de pedidos del cliente (fuente = Pedidos).</summary>
public class HistorialConsumoItemDto
{
    public int PedidoId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal DescuentoMonto { get; set; }
    public decimal PagosCompletados { get; set; }
}
