using FluentValidation;
using PatioElOlvidado.Application.DTOs.Reservas;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Validators;

public class CreateReservaRequestValidator : AbstractValidator<CreateReservaRequest>
{
    public CreateReservaRequestValidator()
    {
        RuleFor(x => x.MesaId).GreaterThan(0);
        RuleFor(x => x.Fecha)
            .Must(f => f != default)
            .WithMessage("La fecha es obligatoria.");
        RuleFor(x => x.Personas).GreaterThan(0);
        RuleFor(x => x.HoraFin)
            .Must((req, fin) => fin > req.HoraInicio)
            .WithMessage("La hora de fin debe ser posterior a la hora de inicio.");
        RuleFor(x => x.ClienteId)
            .GreaterThan(0)
            .When(x => x.ClienteId.HasValue);
    }
}

public class CambiarEstadoReservaRequestValidator : AbstractValidator<CambiarEstadoReservaRequest>
{
    public CambiarEstadoReservaRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty()
            .Must(e => e is nameof(EstadoReserva.Cancelada) or nameof(EstadoReserva.Finalizada))
            .WithMessage("El estado debe ser Cancelada o Finalizada.");
    }
}
