namespace PatioElOlvidado.Domain.Entities;

/// <summary>RF-06 — Registro de entrada/salida (sustituye semántica de Turnos legado).</summary>
public class Fichaje
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public DateTime EntradaUtc { get; set; }
    /// <summary>Null mientras el fichaje está abierto.</summary>
    public DateTime? SalidaUtc { get; set; }
    /// <summary>Horas calculadas al cerrar; null si abierto.</summary>
    public decimal? Horas { get; set; }

    public Empleado? Empleado { get; set; }
}
