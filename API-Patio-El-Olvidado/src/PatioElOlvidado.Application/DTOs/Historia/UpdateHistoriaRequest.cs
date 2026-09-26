namespace PatioElOlvidado.Application.DTOs.Historia;

/// <summary>
/// Cuerpo de PUT /api/historia. No incluye id: la fila que se escribe es siempre la 1.
/// </summary>
public class UpdateHistoriaRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
}
