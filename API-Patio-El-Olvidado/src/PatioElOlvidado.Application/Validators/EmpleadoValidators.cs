using FluentValidation;
using PatioElOlvidado.Application.DTOs.Empleados;

namespace PatioElOlvidado.Application.Validators;

public class CreateEmpleadoRequestValidator : AbstractValidator<CreateEmpleadoRequest>
{
    public CreateEmpleadoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Puesto)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Puesto));

        RuleFor(x => x.Telefono)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.TarifaHora)
            .GreaterThan(0).WithMessage("La tarifa por hora debe ser mayor a 0.");

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .When(x => x.UsuarioId.HasValue);
    }
}

public class UpdateEmpleadoRequestValidator : AbstractValidator<UpdateEmpleadoRequest>
{
    public UpdateEmpleadoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Puesto)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Puesto));

        RuleFor(x => x.Telefono)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.TarifaHora)
            .GreaterThan(0).WithMessage("La tarifa por hora debe ser mayor a 0.");

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .When(x => x.UsuarioId.HasValue);
    }
}

public class CreateLiquidacionRequestValidator : AbstractValidator<CreateLiquidacionRequest>
{
    public CreateLiquidacionRequestValidator()
    {
        RuleFor(x => x.PeriodoHasta)
            .GreaterThanOrEqualTo(x => x.PeriodoDesde)
            .WithMessage("PeriodoHasta debe ser mayor o igual a PeriodoDesde.");
    }
}
