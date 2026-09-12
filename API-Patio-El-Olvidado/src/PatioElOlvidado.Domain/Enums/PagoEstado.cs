namespace PatioElOlvidado.Domain.Enums;

public static class PagoEstado
{
    public const string Completado = "Completado";
    public const string Anulado = "Anulado";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        Completado,
        Anulado
    };

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Todos.Contains(value);
}
