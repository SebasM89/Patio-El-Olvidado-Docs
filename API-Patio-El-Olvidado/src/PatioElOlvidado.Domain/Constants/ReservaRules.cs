namespace PatioElOlvidado.Domain.Constants;

/// <summary>RN-06 — solape de reservas confirmadas en la misma mesa y fecha.</summary>
public static class ReservaRules
{
    /// <summary>
    /// Intervalos cruzados: inicioA &lt; finB y inicioB &lt; finA.
    /// El contacto exacto en un extremo no se considera solape.
    /// </summary>
    public static bool IntervalosSeSolapan(TimeOnly inicioA, TimeOnly finA, TimeOnly inicioB, TimeOnly finB)
        => inicioA < finB && inicioB < finA;
}
