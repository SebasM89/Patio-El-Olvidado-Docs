namespace PatioElOlvidado.Application.DTOs.Inventario;

/// <summary>No incluye cantidad ni unidad: la unidad queda fija al crear y el saldo solo cambia con un movimiento.</summary>
public class UpdateStockItemRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }
}
