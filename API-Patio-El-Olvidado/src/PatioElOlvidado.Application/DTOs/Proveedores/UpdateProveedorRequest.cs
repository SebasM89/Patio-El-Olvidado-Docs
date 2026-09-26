namespace PatioElOlvidado.Application.DTOs.Proveedores;

public class UpdateProveedorRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
}
