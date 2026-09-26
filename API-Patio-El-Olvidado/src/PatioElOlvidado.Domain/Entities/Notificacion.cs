using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Domain.Entities;

/// <summary>
/// Aviso persistido para un usuario. Sin ROWVERSION y sin semilla.
/// La unicidad (UsuarioId, MovimientoStockId) la impone UX_Notificaciones_Usuario_Movimiento.
/// </summary>
public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; }
    public bool Leida { get; set; }
    public DateTime? LeidaUtc { get; set; }
    public DateTime FechaUtc { get; set; }
    public int StockItemId { get; set; }
    public int MovimientoStockId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public StockItem StockItem { get; set; } = null!;
    public MovimientoStock MovimientoStock { get; set; } = null!;
}
