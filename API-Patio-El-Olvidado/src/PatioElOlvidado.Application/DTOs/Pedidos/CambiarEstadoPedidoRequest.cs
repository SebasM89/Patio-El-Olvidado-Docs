namespace PatioElOlvidado.Application.DTOs.Pedidos;

public class CambiarEstadoPedidoRequest
{
    /// <summary>Listo | Entregado | Cancelado</summary>
    public string Estado { get; set; } = string.Empty;
}
