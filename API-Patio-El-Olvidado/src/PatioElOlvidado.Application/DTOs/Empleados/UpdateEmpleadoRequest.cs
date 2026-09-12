namespace PatioElOlvidado.Application.DTOs.Empleados;

public class UpdateEmpleadoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public string? Telefono { get; set; }
    public decimal TarifaHora { get; set; }
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; } = true;
}
