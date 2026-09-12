namespace PatioElOlvidado.Domain.Enums;

public static class PagoMetodo
{
    public const string Efectivo = "Efectivo";
    public const string Tarjeta = "Tarjeta";
    public const string Transferencia = "Transferencia";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        Efectivo,
        Tarjeta,
        Transferencia
    };

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Todos.Contains(value);
}
