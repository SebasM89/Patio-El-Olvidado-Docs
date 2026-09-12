namespace PatioElOlvidado.Application.DTOs.Productos;

public class ProductoFilterQuery
{
    public string? Q { get; set; }
    public string? Categoria { get; set; }
    public string? Etiqueta { get; set; }
    /// <summary>Si es true (default), solo productos activos. false incluye inactivos (soft-deleted).</summary>
    public bool? SoloActivos { get; set; }
}
