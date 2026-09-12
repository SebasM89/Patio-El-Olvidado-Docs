using FluentValidation;
using PatioElOlvidado.Application.DTOs.Clientes;

namespace PatioElOlvidado.Application.Validators;

public class CreateClienteRequestValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .MaximumLength(30);

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .When(x => x.UsuarioId.HasValue);
    }
}

public class UpdateClienteRequestValidator : AbstractValidator<UpdateClienteRequest>
{
    public UpdateClienteRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .MaximumLength(30);

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .When(x => x.UsuarioId.HasValue);
    }
}
