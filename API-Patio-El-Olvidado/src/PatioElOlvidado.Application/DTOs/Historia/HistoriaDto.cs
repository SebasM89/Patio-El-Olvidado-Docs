namespace PatioElOlvidado.Application.DTOs.Historia;

public class HistoriaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime? ActualizadoUtc { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
}
