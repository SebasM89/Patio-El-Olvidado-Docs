using FluentValidation;
using PatioElOlvidado.Application.DTOs.Productos;

namespace PatioElOlvidado.Application.Validators;

public class CreateProductoRequestValidator : AbstractValidator<CreateProductoRequest>
{
    public CreateProductoRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(500).When(x => x.Descripcion is not null);
        RuleFor(x => x.Precio).GreaterThan(0);
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Imagen).MaximumLength(500).When(x => x.Imagen is not null);
        RuleFor(x => x.Etiquetas).MaximumLength(300).When(x => x.Etiquetas is not null);
    }
}

public class UpdateProductoRequestValidator : AbstractValidator<UpdateProductoRequest>
{
    public UpdateProductoRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(500).When(x => x.Descripcion is not null);
        RuleFor(x => x.Precio).GreaterThan(0);
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Imagen).MaximumLength(500).When(x => x.Imagen is not null);
        RuleFor(x => x.Etiquetas).MaximumLength(300).When(x => x.Etiquetas is not null);
    }
}
