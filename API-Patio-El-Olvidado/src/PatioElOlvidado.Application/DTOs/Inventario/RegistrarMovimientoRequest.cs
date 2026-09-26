namespace PatioElOlvidado.Application.DTOs.Inventario;

public class RegistrarMovimientoRequest
{
    public string Tipo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string? Motivo { get; set; }
}
