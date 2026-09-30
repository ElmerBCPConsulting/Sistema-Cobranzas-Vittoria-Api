using Cobranzas_Vittoria.ControlPresupuestario.Domain.Model;

namespace Cobranzas_Vittoria.ControlPresupuestario.Domain.Persistence;


/// <summary>Puerto de persistencia de las partidas de una versión.</summary>
public interface IPresupuestoDetalleRepository
{
    Task<IReadOnlyList<PresupuestoDetalle>> ListarPorVersionAsync(int idPresupuestoVersion);
    Task<int> AgregarAsync(PresupuestoDetalle detalle);
    Task ActualizarAsync(PresupuestoDetalle detalle);
    Task EliminarAsync(int idPresupuestoDetalle);
    Task<ResultadoCargaLote> CargarLoteAsync(LotePresupuestario lote);
}
