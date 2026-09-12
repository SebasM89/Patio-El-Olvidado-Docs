using PatioElOlvidado.Application.DTOs.Pagos;

namespace PatioElOlvidado.Application.Interfaces;

public interface ICajaPdfExporter
{
    byte[] Export(CajaDiaDto caja);
}
