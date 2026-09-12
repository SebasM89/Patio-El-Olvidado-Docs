using PatioElOlvidado.Application.DTOs.Pagos;

namespace PatioElOlvidado.Application.Interfaces;

public interface ICajaCsvExporter
{
    byte[] Export(CajaDiaDto caja);
}
