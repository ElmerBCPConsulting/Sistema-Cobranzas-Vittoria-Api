using Cobranzas_Vittoria.ControlPresupuestario.Domain.Model.Consultas;

namespace Cobranzas_Vittoria.ControlPresupuestario.Application.ConsultaPresupuestaria;

/// <summary>Filtros de lectura que acepta cualquier consulta presupuestaria.</summary>
public sealed record ConsultaPresupuestariaQuery(int? IdCentroCosto, int? IdPresupuesto, int? IdPresupuestoVersion,
    int? IdCatalogoPartida, int? IdMoneda, string? EstadoPresupuesto, bool? SoloExcedidos)
{
    public FiltroConsultaPresupuestaria AFiltro() => new FiltroConsultaPresupuestaria(IdCentroCosto, IdPresupuesto,
        IdPresupuestoVersion, IdCatalogoPartida, IdMoneda, EstadoPresupuesto, SoloExcedidos).Normalizado();
}
