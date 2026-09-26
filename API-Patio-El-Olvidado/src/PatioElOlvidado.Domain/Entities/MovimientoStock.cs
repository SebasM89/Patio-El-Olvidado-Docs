using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Domain.Entities;

/// <summary>Movimiento append-only de un StockItem. No ajusta CantidadActual.</summary>
public class MovimientoStock
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public TipoMovimientoStock Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaUtc { get; set; }
    public int RegistradoPorUsuarioId { get; set; }

    public StockItem StockItem { get; set; } = null!;
    public Usuario RegistradoPorUsuario { get; set; } = null!;
}
