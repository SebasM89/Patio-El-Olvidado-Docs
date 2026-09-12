namespace PatioElOlvidado.Domain.Entities;

/// <summary>Resumen diario de cobros (RN-08). Un registro por fecha UTC.</summary>
public class Caja
{
    public int Id { get; set; }
    /// <summary>Fecha del día (UTC Date).</summary>
    public DateOnly Fecha { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }

    public decimal Total => TotalEfectivo + TotalTarjeta + TotalTransferencia;

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
