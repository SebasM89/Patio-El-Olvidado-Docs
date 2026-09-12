using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Clientes;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Constants;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly IPedidoRepository _pedidos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(
        IClienteRepository clientes,
        IPedidoRepository pedidos,
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork)
    {
        _clientes = clientes;
        _pedidos = pedidos;
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteDto?> GetByIdAsync(
        int id,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByIdAsync(id, cancellationToken);
        if (cliente is null)
            return null;

        if (IsCliente(rol) && cliente.UsuarioId != usuarioId)
            return null;

        return Map(cliente);
    }

    public async Task<ClienteDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByUsuarioIdAsync(usuarioId, cancellationToken);
        return cliente is null ? null : Map(cliente);
    }

    public async Task<IReadOnlyList<ClienteDto>> ListAsync(
        ClienteFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _clientes.SearchAsync(filter, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ClienteDto> CreateAsync(
        CreateClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmailUniqueAsync(request.Email, excludeId: null, cancellationToken);
        await EnsureUsuarioLinkAsync(request.UsuarioId, excludeId: null, cancellationToken);

        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Telefono = request.Telefono.Trim(),
            Email = NormalizeOptional(request.Email),
            Visitas = 0,
            UsuarioId = request.UsuarioId,
            Activo = request.Activo
        };

        await _clientes.AddAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(cliente);
    }

    public async Task<ClienteDto> UpdateAsync(
        int id,
        UpdateClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Cliente no encontrado.", StatusCodes.Status404NotFound);

        await EnsureEmailUniqueAsync(request.Email, excludeId: id, cancellationToken);
        await EnsureUsuarioLinkAsync(request.UsuarioId, excludeId: id, cancellationToken);

        cliente.Nombre = request.Nombre.Trim();
        cliente.Telefono = request.Telefono.Trim();
        cliente.Email = NormalizeOptional(request.Email);
        cliente.UsuarioId = request.UsuarioId;
        cliente.Activo = request.Activo;

        await _clientes.UpdateAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(cliente);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Cliente no encontrado.", StatusCodes.Status404NotFound);

        if (!cliente.Activo)
            return;

        cliente.Activo = false;
        await _clientes.UpdateAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialConsumoItemDto>> GetHistorialAsync(
        int clienteId,
        int usuarioId,
        string rol,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByIdAsync(clienteId, cancellationToken)
            ?? throw new AppException("Cliente no encontrado.", StatusCodes.Status404NotFound);

        if (IsCliente(rol) && cliente.UsuarioId != usuarioId)
            throw new AppException("Cliente no encontrado.", StatusCodes.Status404NotFound);

        return await BuildHistorialAsync(clienteId, cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialConsumoItemDto>> GetMeHistorialAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clientes.GetByUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new AppException(
                "No hay perfil de cliente vinculado a este usuario.",
                StatusCodes.Status404NotFound);

        return await BuildHistorialAsync(cliente.Id, cancellationToken);
    }

    private async Task<IReadOnlyList<HistorialConsumoItemDto>> BuildHistorialAsync(
        int clienteId,
        CancellationToken cancellationToken)
    {
        var pedidos = await _pedidos.ListByClienteIdAsync(clienteId, cancellationToken);
        return pedidos.Select(p =>
        {
            var pagosCompletados = p.Pagos
                .Where(x => x.Estado == PagoEstado.Completado)
                .Sum(x => x.Monto);
            return new HistorialConsumoItemDto
            {
                PedidoId = p.Id,
                FechaCreacion = p.FechaCreacion,
                Tipo = p.Tipo,
                Estado = p.Estado,
                Subtotal = p.Subtotal,
                Total = p.Total,
                DescuentoMonto = FidelizacionRules.CalcularDescuentoMonto(p.Subtotal, p.Total),
                PagosCompletados = pagosCompletados
            };
        }).ToList();
    }

    private async Task EnsureEmailUniqueAsync(
        string? email,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeOptional(email);
        if (normalized is null)
            return;

        if (await _clientes.EmailExistsAsync(normalized, excludeId, cancellationToken))
            throw new AppException("Ya existe un cliente con ese email.", StatusCodes.Status409Conflict);
    }

    private async Task EnsureUsuarioLinkAsync(
        int? usuarioId,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (!usuarioId.HasValue)
            return;

        var usuario = await _usuarios.GetByIdAsync(usuarioId.Value, cancellationToken)
            ?? throw new AppException("Usuario no encontrado.", StatusCodes.Status400BadRequest);

        if (!string.Equals(usuario.Rol?.Nombre, RolesSistema.Cliente, StringComparison.Ordinal))
            throw new AppException(
                "Solo se puede vincular un usuario con rol Cliente.",
                StatusCodes.Status400BadRequest);

        if (await _clientes.UsuarioIdExistsAsync(usuarioId.Value, excludeId, cancellationToken))
            throw new AppException(
                "Ese usuario ya está vinculado a otro cliente.",
                StatusCodes.Status409Conflict);
    }

    private static ClienteDto Map(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Telefono = cliente.Telefono,
        Email = cliente.Email,
        Visitas = cliente.Visitas,
        UsuarioId = cliente.UsuarioId,
        Activo = cliente.Activo
    };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsCliente(string rol)
        => string.Equals(rol, RolesSistema.Cliente, StringComparison.Ordinal);

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
