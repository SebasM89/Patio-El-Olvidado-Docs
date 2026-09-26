using FluentValidation;
using PatioElOlvidado.Application.DTOs.Inventario;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Validators;

public class CreateStockItemRequestValidator : AbstractValidator<CreateStockItemRequest>
{
    public CreateStockItemRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Descripcion));

        RuleFor(x => x.Unidad)
            .NotEmpty()
            .Must(u => InventarioEnumParser.TryParseUnidad(u, out _))
            .WithMessage("La unidad debe ser Unidad, Kg o L.");

        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.")
            .PrecisionScale(12, 3, ignoreTrailingZeros: true)
            .WithMessage("El stock mínimo admite hasta 3 decimales.");

        RuleFor(x => x.CantidadInicial)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad inicial no puede ser negativa.")
            .PrecisionScale(12, 3, ignoreTrailingZeros: true)
            .WithMessage("La cantidad inicial admite hasta 3 decimales.");
    }
}

public class UpdateStockItemRequestValidator : AbstractValidator<UpdateStockItemRequest>
{
    public UpdateStockItemRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Descripcion));

        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.")
            .PrecisionScale(12, 3, ignoreTrailingZeros: true)
            .WithMessage("El stock mínimo admite hasta 3 decimales.");
    }
}

public class RegistrarMovimientoRequestValidator : AbstractValidator<RegistrarMovimientoRequest>
{
    public RegistrarMovimientoRequestValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(t => InventarioEnumParser.TryParseTipo(t, out _))
            .WithMessage("El tipo debe ser Entrada o Salida.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.")
            .PrecisionScale(12, 3, ignoreTrailingZeros: true)
            .WithMessage("La cantidad admite hasta 3 decimales.");

        RuleFor(x => x.Motivo)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Motivo));

        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor no existe o no está activo.")
            .When(x => x.ProveedorId.HasValue);
    }
}

internal static class InventarioEnumParser
{
    public static bool TryParseUnidad(string? raw, out UnidadStock unidad)
    {
        unidad = default;
        if (!TryName(raw, out var name))
            return false;
        return Enum.TryParse(name, ignoreCase: true, out unidad) && Enum.IsDefined(unidad);
    }

    public static bool TryParseTipo(string? raw, out TipoMovimientoStock tipo)
    {
        tipo = default;
        if (!TryName(raw, out var name))
            return false;
        return Enum.TryParse(name, ignoreCase: true, out tipo) && Enum.IsDefined(tipo);
    }

    private static bool TryName(string? raw, out string name)
    {
        name = raw?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Any(char.IsDigit))
            return false;
        return true;
    }
}
