using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Domain.Entities;

/// <summary>
/// Ítem de stock. No referencia Productos.
/// <see cref="Version"/> es ROWVERSION de concurrencia.
/// </summary>
public class StockItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public UnidadStock Unidad { get; set; }
    public decimal CantidadActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;
    public byte[] Version { get; set; } = Array.Empty<byte>();

    public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
}
