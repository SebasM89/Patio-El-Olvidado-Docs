namespace PatioElOlvidado.Application.DTOs.Pedidos;

public class PedidoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? ClienteId { get; set; }
    public int CreadoPorUsuarioId { get; set; }
    public List<DetallePedidoDto> Detalles { get; set; } = new();
}
