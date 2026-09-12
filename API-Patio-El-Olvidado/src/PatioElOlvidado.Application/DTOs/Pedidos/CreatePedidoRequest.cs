namespace PatioElOlvidado.Application.DTOs.Pedidos;

public class CreatePedidoRequest
{
    /// <summary>Local | ParaLlevar</summary>
    public string Tipo { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public List<DetallePedidoLineRequest> Detalles { get; set; } = new();
}
