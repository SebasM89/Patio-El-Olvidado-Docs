using FluentValidation;
using PatioElOlvidado.Application.DTOs.Proveedores;

namespace PatioElOlvidado.Application.Validators;

public class CreateProveedorRequestValidator : AbstractValidator<CreateProveedorRequest>
{
    public CreateProveedorRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Contacto)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Contacto));

        RuleFor(x => x.Telefono)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .EmailAddress().WithMessage("El email no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Notas)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Notas));
    }
}

public class UpdateProveedorRequestValidator : AbstractValidator<UpdateProveedorRequest>
{
    public UpdateProveedorRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Contacto)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Contacto));

        RuleFor(x => x.Telefono)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .EmailAddress().WithMessage("El email no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Notas)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Notas));
    }
}
