namespace PatioElOlvidado.Domain.Entities;

/// <summary>
/// Catálogo de proveedores. Sin CUIT, sin ROWVERSION y sin semilla.
/// </summary>
public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
}
