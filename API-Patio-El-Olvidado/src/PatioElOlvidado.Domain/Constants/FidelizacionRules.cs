namespace PatioElOlvidado.Domain.Constants;

/// <summary>
/// RN-05: la quinta visita genera descuento automático.
/// Porcentaje no documentado en docs → constante de dominio 10% hasta aclaración de producto.
/// </summary>
public static class FidelizacionRules
{
    public const decimal DescuentoFidelizacionPorcentaje = 0.10m;

    /// <summary>
    /// Aplica cuando Visitas % 5 == 4 (la próxima visita completada será múltiplo de 5).
    /// </summary>
    public static bool AplicaDescuento(int visitas)
        => visitas >= 0 && visitas % 5 == 4;

    public static decimal CalcularTotal(decimal subtotal, int visitas)
    {
        if (!AplicaDescuento(visitas))
            return decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);

        var total = subtotal * (1m - DescuentoFidelizacionPorcentaje);
        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalcularDescuentoMonto(decimal subtotal, decimal total)
        => decimal.Round(Math.Max(0, subtotal - total), 2, MidpointRounding.AwayFromZero);
}
