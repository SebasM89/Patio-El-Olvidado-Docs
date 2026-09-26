namespace PatioElOlvidado.Application.DTOs.Usuarios;

public class UpdateUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string? Password { get; set; }
}
