using FluentValidation;
using PatioElOlvidado.Application.DTOs.Promociones;

namespace PatioElOlvidado.Application.Validators;

public class CreatePromocionRequestValidator : AbstractValidator<CreatePromocionRequest>
{
    public CreatePromocionRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre admite hasta 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción admite hasta 500 caracteres.");

        RuleFor(x => x.VigenteHasta)
            .GreaterThanOrEqualTo(x => x.VigenteDesde)
            .WithMessage("La vigencia hasta no puede ser anterior a la vigencia desde.");
    }
}

public class UpdatePromocionRequestValidator : AbstractValidator<UpdatePromocionRequest>
{
    public UpdatePromocionRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre admite hasta 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(500).WithMessage("La descripción admite hasta 500 caracteres.");

        RuleFor(x => x.VigenteHasta)
            .GreaterThanOrEqualTo(x => x.VigenteDesde)
            .WithMessage("La vigencia hasta no puede ser anterior a la vigencia desde.");
    }
}
