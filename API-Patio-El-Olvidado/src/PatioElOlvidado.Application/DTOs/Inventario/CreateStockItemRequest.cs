namespace PatioElOlvidado.Application.DTOs.Inventario;

public class CreateStockItemRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public decimal StockMinimo { get; set; }
    public decimal CantidadInicial { get; set; }
    public bool Activo { get; set; } = true;
}
