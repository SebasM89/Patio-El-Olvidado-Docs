namespace PatioElOlvidado.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }
    /// <summary>Local | ParaLlevar</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>EnPreparacion | Listo | Entregado | Cancelado</summary>
    public string Estado { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    /// <summary>Opcional: pedido anónimo / sin fidelizar.</summary>
    public int? ClienteId { get; set; }
    /// <summary>True cuando el cobro completo ya incrementó Cliente.Visitas (idempotente).</summary>
    public bool VisitaContabilizada { get; set; }
    public int CreadoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Cliente? Cliente { get; set; }
    public Usuario CreadoPorUsuario { get; set; } = null!;
    public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
