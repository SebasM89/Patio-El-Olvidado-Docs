using FluentValidation;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Validators;

public class CreateUsuarioRequestValidator : AbstractValidator<CreateUsuarioRequest>
{
    public CreateUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no es válido.")
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.RolId)
            .GreaterThan(0).WithMessage("El rol es obligatorio.");
    }
}

public class UpdateUsuarioRequestValidator : AbstractValidator<UpdateUsuarioRequest>
{
    public UpdateUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no es válido.")
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));

        RuleFor(x => x.RolId)
            .GreaterThan(0).WithMessage("El rol es obligatorio.");
    }
}

public class CambiarEstadoUsuarioRequestValidator : AbstractValidator<CambiarEstadoUsuarioRequest>
{
    public CambiarEstadoUsuarioRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty()
            .Must(EsEstadoAdministrable)
            .WithMessage("El estado debe ser Activo o Inactivo.");
    }

    private static bool EsEstadoAdministrable(string? estado)
        => string.Equals(estado?.Trim(), UsuarioEstado.Activo, StringComparison.OrdinalIgnoreCase)
           || string.Equals(estado?.Trim(), UsuarioEstado.Inactivo, StringComparison.OrdinalIgnoreCase);
}
