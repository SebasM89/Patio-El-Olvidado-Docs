namespace PatioElOlvidado.Application.DTOs.Clientes;

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Visitas { get; set; }
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; }
}
