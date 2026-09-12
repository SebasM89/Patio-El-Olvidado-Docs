namespace PatioElOlvidado.Application.DTOs.Clientes;

public class ClienteFilterQuery
{
    public string? Q { get; set; }
    /// <summary>null = todos; true/false filtra por Activo.</summary>
    public bool? Activo { get; set; }
}
