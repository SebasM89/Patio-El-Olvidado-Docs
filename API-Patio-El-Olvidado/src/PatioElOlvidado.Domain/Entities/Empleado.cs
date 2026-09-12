namespace PatioElOlvidado.Domain.Entities;

/// <summary>RF-06 — Empleado vinculado opcionalmente a usuario rol Empleado.</summary>
public class Empleado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public string? Telefono { get; set; }
    /// <summary>Tarifa horaria (solo Admin escribe).</summary>
    public decimal TarifaHora { get; set; }
    /// <summary>Acumulado denormalizado; se incrementa al cerrar fichaje.</summary>
    public decimal HorasTrabajadas { get; set; }
    /// <summary>Usuario rol Empleado (obligatorio para /me y fichaje propio).</summary>
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
    public ICollection<Fichaje> Fichajes { get; set; } = new List<Fichaje>();
    public ICollection<Liquidacion> Liquidaciones { get; set; } = new List<Liquidacion>();
}
