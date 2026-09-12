namespace PatioElOlvidado.Domain.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid => UsedAt is null && DateTime.UtcNow < ExpiresAt;

    public Usuario Usuario { get; set; } = null!;
}
