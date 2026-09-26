namespace PatioElOlvidado.Domain.Enums;

/// <summary>
/// CU08. Se persiste como NVARCHAR(30) con el nombre del valor
/// (Confirmada, Cancelada, Finalizada). No almacenar como entero.
/// </summary>
public enum EstadoReserva
{
    Confirmada,
    Cancelada,
    Finalizada
}
