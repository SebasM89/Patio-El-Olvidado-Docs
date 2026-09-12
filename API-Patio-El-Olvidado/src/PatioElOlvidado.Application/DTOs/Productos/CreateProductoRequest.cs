namespace PatioElOlvidado.Application.DTOs.Productos;

public class CreateProductoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string? Imagen { get; set; }
    public string? Etiquetas { get; set; }
    public bool Activo { get; set; } = true;
}
