using FluentValidation;
using PatioElOlvidado.Application.DTOs.Reportes;

namespace PatioElOlvidado.Application.Validators;

public class ReporteRangoQueryValidator : AbstractValidator<ReporteRangoQuery>
{
    public const int MaxDiasInclusive = 366;

    public ReporteRangoQueryValidator()
    {
        RuleFor(x => x.Hasta)
            .GreaterThanOrEqualTo(x => x.Desde)
            .WithMessage("Desde debe ser menor o igual a Hasta.");

        RuleFor(x => x)
            .Must(q => q.Hasta.DayNumber - q.Desde.DayNumber + 1 <= MaxDiasInclusive)
            .WithMessage($"El rango no puede superar {MaxDiasInclusive} días.");
    }
}
