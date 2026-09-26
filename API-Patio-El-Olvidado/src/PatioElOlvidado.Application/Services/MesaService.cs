using PatioElOlvidado.Application.DTOs.Mesas;
using PatioElOlvidado.Application.Interfaces;
using PatioElOlvidado.Domain.Entities;

namespace PatioElOlvidado.Application.Services;

public class MesaService : IMesaService
{
    private readonly IMesaRepository _mesas;

    public MesaService(IMesaRepository mesas) => _mesas = mesas;

    public async Task<IReadOnlyList<MesaDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var mesas = await _mesas.ListAsync(cancellationToken);
        return mesas.Select(Map).ToList();
    }

    private static MesaDto Map(Mesa mesa) => new()
    {
        Id = mesa.Id,
        Numero = mesa.Numero,
        Capacidad = mesa.Capacidad,
        Ubicacion = mesa.Ubicacion
    };
}
