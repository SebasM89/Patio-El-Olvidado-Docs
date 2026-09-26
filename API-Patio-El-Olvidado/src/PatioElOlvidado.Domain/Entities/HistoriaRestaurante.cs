namespace PatioElOlvidado.Domain.Entities;

/// <summary>
/// Texto único del restaurante. La fila válida es Id = 1 (sin IDENTITY).
/// Sin Activo y sin ROWVERSION. La semilla vive en SQL.
/// </summary>
public class HistoriaRestaurante
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime? ActualizadoUtc { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }

    public Usuario? ActualizadoPorUsuario { get; set; }
}
