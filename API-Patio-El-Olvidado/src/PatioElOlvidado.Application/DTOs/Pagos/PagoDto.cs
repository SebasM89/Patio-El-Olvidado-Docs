namespace PatioElOlvidado.Application.DTOs.Pagos;

public class PagoDto
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public int CajaId { get; set; }
}
