using Microsoft.EntityFrameworkCore;
using PatioElOlvidado.Application.DTOs.Pedidos;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;
using PatioElOlvidado.Infrastructure.Persistence;

namespace PatioElOlvidado.Infrastructure.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _db;

    public PedidoRepository(AppDbContext db) => _db = db;

    public Task<Pedido?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default)
        => _db.Pedidos
            .Include(p => p.Detalles)
            .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pedido>> SearchAsync(
        PedidoFilterQuery filter,
        int? soloCreadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Pedidos
            .Include(p => p.Detalles)
            .ThenInclude(d => d.Producto)
            .AsQueryable();

        if (soloCreadoPorUsuarioId.HasValue)
            query = query.Where(p => p.CreadoPorUsuarioId == soloCreadoPorUsuarioId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Estado))
        {
            var estado = filter.Estado.Trim();
            query = query.Where(p => p.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(filter.Tipo))
        {
            var tipo = filter.Tipo.Trim();
            query = query.Where(p => p.Tipo == tipo);
        }

        if (filter.Desde.HasValue)
        {
            var desde = filter.Desde.Value;
            query = query.Where(p => p.FechaCreacion >= desde);
        }

        if (filter.Hasta.HasValue)
        {
            var hasta = filter.Hasta.Value;
            query = query.Where(p => p.FechaCreacion <= hasta);
        }

        return await query
            .OrderByDescending(p => p.FechaCreacion)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pedido>> ListByClienteIdAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Pedidos
            .Include(p => p.Pagos)
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.FechaCreacion)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        await _db.Pedidos.AddAsync(pedido, cancellationToken);
    }

    public Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        _db.Pedidos.Update(pedido);
        return Task.CompletedTask;
    }

    public void RemoveDetalles(IEnumerable<DetallePedido> detalles)
    {
        _db.DetallePedidos.RemoveRange(detalles);
    }
}
