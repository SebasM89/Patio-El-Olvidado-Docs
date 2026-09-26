namespace PatioElOlvidado.Domain.Entities;

/// <summary>
/// Catálogo de promociones. Sin FKs, sin ROWVERSION, sin semilla y sin lógica de descuento.
/// La baja es lógica (Activo = false).
/// </summary>
public class Promocion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly VigenteDesde { get; set; }
    public DateOnly VigenteHasta { get; set; }
    public bool Activo { get; set; } = true;
}
