namespace PatioElOlvidado.Application.DTOs.Clientes;

public class CreateClienteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; } = true;
}
