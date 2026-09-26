namespace PatioElOlvidado.Application.DTOs.Promociones;

public class UpdatePromocionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly VigenteDesde { get; set; }
    public DateOnly VigenteHasta { get; set; }
    public bool Activo { get; set; }
}
