namespace PatioElOlvidado.Domain.Entities;

/// <summary>RF-06 — Liquidación interna MVP (horas × tarifa); sin AFIP/PDF.</summary>
public class Liquidacion
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public DateOnly PeriodoDesde { get; set; }
    public DateOnly PeriodoHasta { get; set; }
    public decimal Horas { get; set; }
    public decimal TarifaHoraSnapshot { get; set; }
    public decimal Monto { get; set; }
    public DateTime GeneradaEnUtc { get; set; }
    public int GeneradaPorUsuarioId { get; set; }

    public Empleado? Empleado { get; set; }
    public Usuario? GeneradaPorUsuario { get; set; }
}
