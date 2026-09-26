namespace PatioElOlvidado.Application.DTOs.Notificaciones;

public class NotificacionDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaUtc { get; set; }
    public DateTime? LeidaUtc { get; set; }
    public int StockItemId { get; set; }
    public int MovimientoStockId { get; set; }
}
