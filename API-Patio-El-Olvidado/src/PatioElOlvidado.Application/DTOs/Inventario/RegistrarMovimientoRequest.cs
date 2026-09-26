namespace PatioElOlvidado.Application.DTOs.Inventario;

public class RegistrarMovimientoRequest
{
    public string Tipo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Opcional. Solo entradas. Null mantiene el alta sin proveedor.</summary>
    public int? ProveedorId { get; set; }
}
