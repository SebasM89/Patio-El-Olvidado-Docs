namespace PatioElOlvidado.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string Categoria { get; set; } = string.Empty;
    /// <summary>URL de imagen (sin upload binario en MVP).</summary>
    public string? Imagen { get; set; }
    /// <summary>Etiquetas separadas por coma (ej. "vegano,sin gluten").</summary>
    public string? Etiquetas { get; set; }
    public bool Activo { get; set; } = true;
}
