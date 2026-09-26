namespace PatioElOlvidado.Application.DTOs.Inventario;

public class MovimientoStockDto
{
    public int Id { get; set; }
    public int StockItemId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaUtc { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public string? RegistradoPorNombre { get; set; }
}
