namespace PatioElOlvidado.Domain.Enums;

public static class PedidoEstado
{
    public const string EnPreparacion = "EnPreparacion";
    public const string Listo = "Listo";
    public const string Entregado = "Entregado";
    public const string Cancelado = "Cancelado";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        EnPreparacion,
        Listo,
        Entregado,
        Cancelado
    };

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Todos.Contains(value);

    public static bool IsTerminal(string estado)
        => estado is Entregado or Cancelado;
}
