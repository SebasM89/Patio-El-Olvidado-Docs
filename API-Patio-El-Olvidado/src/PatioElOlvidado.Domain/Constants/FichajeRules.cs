namespace PatioElOlvidado.Domain.Constants;

/// <summary>RF-06 — Cálculo de horas al cerrar un fichaje.</summary>
public static class FichajeRules
{
    public static decimal CalcularHoras(DateTime entradaUtc, DateTime salidaUtc)
    {
        var span = salidaUtc - entradaUtc;
        if (span.TotalSeconds <= 0)
            return 0m;

        return decimal.Round((decimal)span.TotalHours, 4, MidpointRounding.AwayFromZero);
    }

    public static decimal CalcularMonto(decimal horas, decimal tarifaHora)
        => decimal.Round(horas * tarifaHora, 2, MidpointRounding.AwayFromZero);
}
