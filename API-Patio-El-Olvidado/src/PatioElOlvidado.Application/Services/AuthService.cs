using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.DTOs.Auth;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Application.Options;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Domain.Enums;

namespace PatioElOlvidado.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly IUsuarioRepository _usuarios;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IUsuarioRepository usuarios,
        IRefreshTokenRepository refreshTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions)
    {
        _usuarios = usuarios;
        _refreshTokens = refreshTokens;
        _passwordResetTokens = passwordResetTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarios.GetByEmailAsync(email, cancellationToken)
            ?? throw new AuthException("Credenciales inválidas.", 401);

        if (usuario.Estado == UsuarioEstado.Inactivo)
            throw new AuthException("Usuario inactivo.", 403);

        if (usuario.BloqueadoHasta is not null && usuario.BloqueadoHasta > DateTime.UtcNow)
            throw new AuthException("Cuenta bloqueada temporalmente. Intente nuevamente en unos segundos.", 423);

        if (!_passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxFailedAttempts)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.Add(LockoutDuration);
                usuario.IntentosFallidos = 0;
            }

            await _usuarios.UpdateAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthException("Credenciales inválidas.", 401);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = DateTime.UtcNow;
        await _usuarios.UpdateAsync(usuario, cancellationToken);

        return await IssueTokensAsync(usuario, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new AuthException("Refresh token inválido.", 401);

        if (!stored.IsActive)
            throw new AuthException("Refresh token expirado o revocado.", 401);

        var usuario = stored.Usuario;
        if (usuario.Estado == UsuarioEstado.Inactivo)
            throw new AuthException("Usuario inactivo.", 403);

        stored.RevokedAt = DateTime.UtcNow;
        var newRefreshValue = _jwtTokenService.CreateRefreshTokenValue();
        stored.ReplacedByToken = newRefreshValue;
        await _refreshTokens.UpdateAsync(stored, cancellationToken);

        var (accessToken, accessExpires) = _jwtTokenService.CreateAccessToken(usuario);
        var refreshEntity = new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = newRefreshValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };
        await _refreshTokens.AddAsync(refreshEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshValue,
            AccessTokenExpiresAt = accessExpires,
            Usuario = MapUsuario(usuario)
        };
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (stored is null || !stored.IsActive)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.UpdateAsync(stored, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        const string genericMessage = "Si el email existe, recibirás instrucciones para restablecer la contraseña.";
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarios.GetByEmailAsync(email, cancellationToken);

        if (usuario is not null && usuario.Estado != UsuarioEstado.Inactivo)
        {
            await _passwordResetTokens.InvalidateActiveTokensAsync(usuario.Id, cancellationToken);

            var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var resetToken = new PasswordResetToken
            {
                UsuarioId = usuario.Id,
                Token = tokenValue,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime)
            };
            await _passwordResetTokens.AddAsync(resetToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var body = $"Usá este token para restablecer tu contraseña (válido 1 hora):{Environment.NewLine}{tokenValue}";
            await _emailSender.SendAsync(usuario.Email, "Restablecer contraseña — Patio El Olvidado", body, cancellationToken);
        }

        return new MessageResponse { Message = genericMessage };
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var resetToken = await _passwordResetTokens.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new AuthException("Token de restablecimiento inválido o expirado.", 400);

        if (!resetToken.IsValid)
            throw new AuthException("Token de restablecimiento inválido o expirado.", 400);

        var usuario = resetToken.Usuario;
        usuario.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        resetToken.UsedAt = DateTime.UtcNow;

        await _usuarios.UpdateAsync(usuario, cancellationToken);
        await _passwordResetTokens.UpdateAsync(resetToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Contraseña actualizada correctamente." };
    }

    private async Task<AuthResponse> IssueTokensAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpires) = _jwtTokenService.CreateAccessToken(usuario);
        var refreshValue = _jwtTokenService.CreateRefreshTokenValue();
        var refreshEntity = new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = refreshValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };

        await _refreshTokens.AddAsync(refreshEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshValue,
            AccessTokenExpiresAt = accessExpires,
            Usuario = MapUsuario(usuario)
        };
    }

    private static UsuarioAuthDto MapUsuario(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol.Nombre
    };
}
