using FluentValidation;
using PatioElOlvidado.Application.DTOs.Historia;

namespace PatioElOlvidado.Application.Validators;

public class UpdateHistoriaRequestValidator : AbstractValidator<UpdateHistoriaRequest>
{
    public const int TituloMax = 120;
    public const int TextoMax = 4000;

    public UpdateHistoriaRequestValidator()
    {
        RuleFor(x => x.Titulo).Cascade(CascadeMode.Stop)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("El título es obligatorio.")
            .Must(t => t!.Trim().Length <= TituloMax)
            .WithMessage($"El título admite hasta {TituloMax} caracteres.");

        RuleFor(x => x.Texto).Cascade(CascadeMode.Stop)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("El texto es obligatorio.")
            .Must(t => t!.Trim().Length <= TextoMax)
            .WithMessage($"El texto admite hasta {TextoMax} caracteres.");
    }
}
