using PatioElOlvidado.Application.DTOs.Empleados;

namespace PatioElOlvidado.Application.Interfaces;

public interface IEmpleadoService
{
    Task<EmpleadoDto?> GetByIdAsync(int id, int usuarioId, string rol, CancellationToken cancellationToken = default);
    Task<EmpleadoDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmpleadoDto>> ListAsync(EmpleadoFilterQuery filter, CancellationToken cancellationToken = default);
    Task<EmpleadoDto> CreateAsync(CreateEmpleadoRequest request, CancellationToken cancellationToken = default);
    Task<EmpleadoDto> UpdateAsync(int id, UpdateEmpleadoRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FichajeDto>> ListFichajesAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FichajeDto>> ListMeFichajesAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<FichajeDto> RegistrarEntradaAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<FichajeDto> RegistrarEntradaMeAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<FichajeDto> RegistrarSalidaAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<FichajeDto> RegistrarSalidaMeAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<LiquidacionPreviewDto> PreviewLiquidacionAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
    Task<LiquidacionPreviewDto> PreviewMeLiquidacionAsync(
        int usuarioId,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LiquidacionDto>> ListLiquidacionesAsync(int empleadoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LiquidacionDto>> ListMeLiquidacionesAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<LiquidacionDto> GenerarLiquidacionAsync(
        int empleadoId,
        CreateLiquidacionRequest request,
        int generadaPorUsuarioId,
        CancellationToken cancellationToken = default);
}
