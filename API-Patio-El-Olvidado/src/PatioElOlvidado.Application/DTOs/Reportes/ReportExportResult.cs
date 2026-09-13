namespace PatioElOlvidado.Application.DTOs.Reportes;

public class ReportExportResult
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
