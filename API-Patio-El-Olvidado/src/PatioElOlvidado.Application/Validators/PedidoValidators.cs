using FluentValidation;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Validators;

public class DetallePedidoLineRequestValidator : AbstractValidator<DetallePedidoLineRequest>
{
    public DetallePedidoLineRequestValidator()
    {
        RuleFor(x => x.ProductoId).GreaterThan(0);
        RuleFor(x => x.Cantidad).GreaterThan(0);
    }
}

public class CreatePedidoRequestValidator : AbstractValidator<CreatePedidoRequest>
{
    public CreatePedidoRequestValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(PedidoTipo.IsValid)
            .WithMessage("Tipo debe ser Local o ParaLlevar.");
        RuleFor(x => x.ClienteId)
            .GreaterThan(0)
            .When(x => x.ClienteId.HasValue);
        RuleFor(x => x.Detalles)
            .NotEmpty()
            .WithMessage("El pedido debe tener al menos una línea.");
        RuleForEach(x => x.Detalles).SetValidator(new DetallePedidoLineRequestValidator());
    }
}

public class UpdatePedidoRequestValidator : AbstractValidator<UpdatePedidoRequest>
{
    public UpdatePedidoRequestValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(PedidoTipo.IsValid)
            .WithMessage("Tipo debe ser Local o ParaLlevar.");
        RuleFor(x => x.ClienteId)
            .GreaterThan(0)
            .When(x => x.ClienteId.HasValue);
        RuleFor(x => x.Detalles)
            .NotEmpty()
            .WithMessage("El pedido debe tener al menos una línea.");
        RuleForEach(x => x.Detalles).SetValidator(new DetallePedidoLineRequestValidator());
    }
}

public class CambiarEstadoPedidoRequestValidator : AbstractValidator<CambiarEstadoPedidoRequest>
{
    public CambiarEstadoPedidoRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty()
            .Must(e => e is PedidoEstado.Listo or PedidoEstado.Entregado or PedidoEstado.Cancelado)
            .WithMessage("Estado debe ser Listo, Entregado o Cancelado.");
    }
}
