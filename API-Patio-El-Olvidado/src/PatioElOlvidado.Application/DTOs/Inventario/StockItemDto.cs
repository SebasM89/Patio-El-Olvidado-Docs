namespace PatioElOlvidado.Application.DTOs.Inventario;

public class StockItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public decimal CantidadActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }
    public bool EnAlerta { get; set; }
}
