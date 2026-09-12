namespace PatioElOlvidado.Domain.Entities;

/// <summary>RF-05 — Cliente de mostrador o vinculado a usuario rol Cliente.</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    /// <summary>Visitas contabilizadas tras cobro completo (RN-05).</summary>
    public int Visitas { get; set; }
    /// <summary>Usuario rol Cliente opcional (null = walk-in).</summary>
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
