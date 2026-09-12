namespace PatioElOlvidado.Application.DTOs.Caja;

public class CajaExportResult
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
