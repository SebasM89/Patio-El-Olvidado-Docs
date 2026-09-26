namespace PatioElOlvidado.Application.DTOs.Promociones;

public class CreatePromocionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly VigenteDesde { get; set; }
    public DateOnly VigenteHasta { get; set; }
}
