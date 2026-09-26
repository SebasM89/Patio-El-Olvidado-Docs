namespace PatioElOlvidado.Domain.Entities;

/// <summary>CU08 — mesa del salón. Catálogo fijo: sin Activo y sin CRUD.</summary>
public class Mesa
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int Capacidad { get; set; }
    public string? Ubicacion { get; set; }

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
