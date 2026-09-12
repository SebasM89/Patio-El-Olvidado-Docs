namespace PatioElOlvidado.Application.DTOs.Empleados;

public class EmpleadoFilterQuery
{
    public string? Q { get; set; }
    /// <summary>null = todos; true/false filtra por Activo.</summary>
    public bool? Activo { get; set; }
}
