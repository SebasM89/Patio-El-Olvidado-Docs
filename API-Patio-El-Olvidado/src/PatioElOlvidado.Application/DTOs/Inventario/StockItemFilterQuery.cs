namespace PatioElOlvidado.Application.DTOs.Inventario;

public class StockItemFilterQuery
{
    public string? Q { get; set; }
    public bool? Activo { get; set; }
    public bool? EnAlerta { get; set; }
}
