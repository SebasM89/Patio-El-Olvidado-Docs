namespace PatioElOlvidado.Domain.Enums;

public static class PedidoTipo
{
    public const string Local = "Local";
    public const string ParaLlevar = "ParaLlevar";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        Local,
        ParaLlevar
    };

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Todos.Contains(value);
}
