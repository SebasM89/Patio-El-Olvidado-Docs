namespace PatioElOlvidado.Domain.Entities;

public class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }

    public Rol Rol { get; set; } = null!;
    public Permiso Permiso { get; set; } = null!;
}
