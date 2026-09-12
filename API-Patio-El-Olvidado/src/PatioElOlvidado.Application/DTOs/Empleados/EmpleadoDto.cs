namespace PatioElOlvidado.Application.DTOs.Empleados;

public class EmpleadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public string? Telefono { get; set; }
    public decimal TarifaHora { get; set; }
    public decimal HorasTrabajadas { get; set; }
    public int? UsuarioId { get; set; }
    public bool Activo { get; set; }
}
