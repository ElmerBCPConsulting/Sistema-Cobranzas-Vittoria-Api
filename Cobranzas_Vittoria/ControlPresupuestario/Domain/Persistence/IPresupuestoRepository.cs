using Cobranzas_Vittoria.ControlPresupuestario.Domain.Model;

namespace Cobranzas_Vittoria.ControlPresupuestario.Domain.Persistence;


/// <summary>Puerto de persistencia de presupuestos.</summary>
public interface IPresupuestoRepository
{
    Task<IReadOnlyList<Presupuesto>> ListarAsync(bool? activo, int? idCentroCosto, int? idMoneda, string? busqueda);
    Task<Presupuesto?> ObtenerAsync(int idPresupuesto);
    Task<PresupuestoCreado> CrearAsync(Presupuesto presupuesto, string usuario);
    Task ActualizarAsync(Presupuesto presupuesto);
}
