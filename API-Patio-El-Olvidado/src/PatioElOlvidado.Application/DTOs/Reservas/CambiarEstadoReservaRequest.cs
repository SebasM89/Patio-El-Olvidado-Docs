namespace PatioElOlvidado.Application.DTOs.Reservas;

public class CambiarEstadoReservaRequest
{
    /// <summary>Cancelada | Finalizada</summary>
    public string Estado { get; set; } = string.Empty;
}
