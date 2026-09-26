namespace PatioElOlvidado.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la acción dentro de una transacción Serializable en SQL Server.
    /// En proveedores no relacionales (tests InMemory) corre la acción directo.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
