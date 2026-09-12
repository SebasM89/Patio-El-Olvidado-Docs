namespace PatioElOlvidado.Application.DTOs.Pedidos;

public class PedidoFilterQuery
{
    public string? Estado { get; set; }
    public string? Tipo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
