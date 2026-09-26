using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Usuarios;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

/// <summary>Módulo Usuarios — CRUD de cuentas, solo Admin. No crea fichas Cliente/Empleado.</summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IRolRepository _roles;
    private readonly IClienteRepository _clientes;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(
        IUsuarioRepository usuarios,
        IRolRepository roles,
        IClienteRepository clientes,
        IEmpleadoRepository empleados,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _usuarios = usuarios;
        _roles = roles;
        _clientes = clientes;
        _empleados = empleados;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListAsync(
        UsuarioFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _usuarios.SearchAsync(filter, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<UsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.GetByIdAsync(id, cancellationToken);
        return usuario is null ? null : Map(usuario);
    }

    public async Task<IReadOnlyList<RolCatalogoDto>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roles.ListAsync(cancellationToken);
        return roles.Select(r => new RolCatalogoDto { Id = r.Id, Nombre = r.Nombre }).ToList();
    }

    public async Task<UsuarioDto> CreateAsync(
        CreateUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePassword(request.Password, required: true);

        var email = NormalizeEmail(request.Email);
        if (await _usuarios.EmailExistsAsync(email, null, cancellationToken))
            throw new AppException("Ya existe un usuario con ese email.", StatusCodes.Status409Conflict);

        var rol = await _roles.GetByIdAsync(request.RolId, cancellationToken)
            ?? throw new AppException("Rol no encontrado.", StatusCodes.Status400BadRequest);

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RolId = rol.Id,
            Rol = rol,
            Estado = UsuarioEstado.Activo,
            IntentosFallidos = 0
        };

        await _usuarios.AddAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(usuario);
    }

    public async Task<UsuarioDto> UpdateAsync(
        int id,
        UpdateUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Usuario no encontrado.", StatusCodes.Status404NotFound);

        if (!string.IsNullOrWhiteSpace(request.Password))
            EnsurePassword(request.Password, required: false);

        var email = NormalizeEmail(request.Email);
        if (await _usuarios.EmailExistsAsync(email, usuario.Id, cancellationToken))
            throw new AppException("Ya existe un usuario con ese email.", StatusCodes.Status409Conflict);

        var rol = await _roles.GetByIdAsync(request.RolId, cancellationToken)
            ?? throw new AppException("Rol no encontrado.", StatusCodes.Status400BadRequest);

        if (rol.Id != usuario.RolId)
            await EnsureRolChangeAllowedAsync(usuario, rol, cancellationToken);

        usuario.Nombre = request.Nombre.Trim();
        usuario.Email = email;
        usuario.RolId = rol.Id;
        usuario.Rol = rol;
        if (!string.IsNullOrWhiteSpace(request.Password))
            usuario.PasswordHash = _passwordHasher.Hash(request.Password);

        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(usuario);
    }

    public async Task<UsuarioDto> CambiarEstadoAsync(
        int id,
        CambiarEstadoUsuarioRequest request,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Usuario no encontrado.", StatusCodes.Status404NotFound);

        var estado = CanonicalEstado(request.Estado);

        var desactiva = estado == UsuarioEstado.Inactivo
            && !string.Equals(usuario.Estado, UsuarioEstado.Inactivo, StringComparison.Ordinal);

        if (desactiva && usuario.Id == actorUsuarioId)
            throw new AppException(
                "No podés desactivar tu propio usuario.",
                StatusCodes.Status400BadRequest);

        if (desactiva
            && string.Equals(usuario.Rol?.Nombre, RolesSistema.Admin, StringComparison.Ordinal)
            && string.Equals(usuario.Estado, UsuarioEstado.Activo, StringComparison.Ordinal))
        {
            var otros = await _usuarios.CountActiveAdminsAsync(usuario.Id, cancellationToken);
            if (otros == 0)
                throw new AppException(
                    "No se puede desactivar al último administrador activo.",
                    StatusCodes.Status400BadRequest);
        }

        usuario.Estado = estado;
        if (estado == UsuarioEstado.Inactivo)
            await _refreshTokens.RevokeActiveByUsuarioIdAsync(usuario.Id, DateTime.UtcNow, cancellationToken);

        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(usuario);
    }

    private async Task EnsureRolChangeAllowedAsync(
        Usuario usuario,
        Rol nuevoRol,
        CancellationToken cancellationToken)
    {
        var cliente = await _clientes.GetByUsuarioIdAsync(usuario.Id, cancellationToken);
        if (cliente is not null
            && !string.Equals(nuevoRol.Nombre, RolesSistema.Cliente, StringComparison.Ordinal))
        {
            throw new AppException(
                "No se puede cambiar el rol: el usuario está vinculado a un cliente y debe seguir siendo Cliente.",
                StatusCodes.Status409Conflict);
        }

        var empleado = await _empleados.GetByUsuarioIdAsync(usuario.Id, cancellationToken);
        if (empleado is not null
            && !string.Equals(nuevoRol.Nombre, RolesSistema.Empleado, StringComparison.Ordinal))
        {
            throw new AppException(
                "No se puede cambiar el rol: el usuario está vinculado a un empleado y debe seguir siendo Empleado.",
                StatusCodes.Status409Conflict);
        }

        var esUltimoAdminActivo =
            string.Equals(usuario.Rol?.Nombre, RolesSistema.Admin, StringComparison.Ordinal)
            && string.Equals(usuario.Estado, UsuarioEstado.Activo, StringComparison.Ordinal)
            && !string.Equals(nuevoRol.Nombre, RolesSistema.Admin, StringComparison.Ordinal)
            && await _usuarios.CountActiveAdminsAsync(usuario.Id, cancellationToken) == 0;

        if (esUltimoAdminActivo)
            throw new AppException(
                "No se puede cambiar el rol del último administrador activo.",
                StatusCodes.Status400BadRequest);
    }

    private static void EnsurePassword(string password, bool required)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            if (required)
                throw new AppException(
                    "La contraseña es obligatoria y debe tener al menos 8 caracteres.",
                    StatusCodes.Status400BadRequest);
            return;
        }

        if (password.Length < 8)
            throw new AppException(
                "La contraseña debe tener al menos 8 caracteres.",
                StatusCodes.Status400BadRequest);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new AppException("El email es obligatorio.", StatusCodes.Status400BadRequest);

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 150)
            throw new AppException("El email no puede superar 150 caracteres.", StatusCodes.Status400BadRequest);

        return normalized;
    }

    private static string CanonicalEstado(string estado)
    {
        if (string.Equals(estado?.Trim(), UsuarioEstado.Activo, StringComparison.OrdinalIgnoreCase))
            return UsuarioEstado.Activo;
        if (string.Equals(estado?.Trim(), UsuarioEstado.Inactivo, StringComparison.OrdinalIgnoreCase))
            return UsuarioEstado.Inactivo;

        throw new AppException(
            "El estado debe ser Activo o Inactivo.",
            StatusCodes.Status400BadRequest);
    }

    private static UsuarioDto Map(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        RolId = usuario.RolId,
        RolNombre = usuario.Rol?.Nombre ?? string.Empty,
        Estado = usuario.Estado,
        UltimoAcceso = usuario.UltimoAcceso
    };

    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
    }
}
