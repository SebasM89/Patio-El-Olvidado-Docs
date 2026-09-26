namespace PatioElOlvidado.Application.DTOs.Usuarios;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string RolNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? UltimoAcceso { get; set; }
}
