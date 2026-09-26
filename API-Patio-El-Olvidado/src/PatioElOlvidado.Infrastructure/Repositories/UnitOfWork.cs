using System.Data;
using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.Common;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(
                "El ítem de stock fue modificado por otro usuario.",
                409);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
            return await action(cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
