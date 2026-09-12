using FluentValidation;
using PatioElOlvidado.Application.DTOs.Pagos;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Validators;

public class CreatePagoRequestValidator : AbstractValidator<CreatePagoRequest>
{
    public CreatePagoRequestValidator()
    {
        RuleFor(x => x.PedidoId).GreaterThan(0);
        RuleFor(x => x.Metodo)
            .NotEmpty()
            .Must(PagoMetodo.IsValid)
            .WithMessage("Método debe ser Efectivo, Tarjeta o Transferencia.");
        RuleFor(x => x.Monto).GreaterThan(0);
    }
}
