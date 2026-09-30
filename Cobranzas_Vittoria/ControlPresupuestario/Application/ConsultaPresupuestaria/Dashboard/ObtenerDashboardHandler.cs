using Cobranzas_Vittoria.ControlPresupuestario.Domain.Excepciones;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Model.Consultas;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Persistence;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Service;

namespace Cobranzas_Vittoria.ControlPresupuestario.Application.ConsultaPresupuestaria.Dashboard;

/// <summary>
/// Tablero de un centro de costo, compuesto a partir de las vistas: los presupuestos activos con
/// versión aprobada, sus montos vigentes por rubro (vw_ControlPresupuestarioVigente) y la ejecución
/// diaria (vw_EjecucionDiariaPorPartida). Nunca se suman montos de monedas distintas.
/// </summary>
public sealed class ObtenerDashboardHandler
{
    private readonly ICentroCostoRepository _centros;
    private readonly IPresupuestoRepository _presupuestos;
    private readonly IConsultaPresupuestariaRepository _consultas;
    private readonly TimeProvider _reloj;

    public ObtenerDashboardHandler(ICentroCostoRepository centros, IPresupuestoRepository presupuestos,
        IConsultaPresupuestariaRepository consultas, TimeProvider reloj)
    {
        _centros = centros;
        _presupuestos = presupuestos;
        _consultas = consultas;
        _reloj = reloj;
    }

    public async Task<Tablero> HandleAsync(ObtenerDashboardQuery query)
    {
        if (query.IdCentroCosto <= 0)
            throw new ValidacionPresupuestariaException("CAMPO_REQUERIDO", "Selecciona un centro de costo para ver el dashboard.");
        if (query.IdPresupuesto is <= 0)
            throw new ValidacionPresupuestariaException("IDENTIFICADOR_INVALIDO", "El presupuesto indicado no es válido.");

        var centro = await _centros.ObtenerAsync(query.IdCentroCosto)
            ?? throw new CentroCostoNoEncontradoException(query.IdCentroCosto);
        var presupuestos = (await _presupuestos.ListarAsync(true, query.IdCentroCosto, null, null))
            .Where(p => p.TieneVersionAprobada && (query.IdPresupuesto is null || p.IdPresupuesto == query.IdPresupuesto))
            .ToList();
        if (presupuestos.Select(p => p.IdMoneda).Distinct().Count() > 1)
            throw new ValidacionPresupuestariaException("MONEDAS_MIXTAS",
                "El centro de costo tiene presupuestos en distintas monedas: elige un presupuesto para ver el dashboard.");

        var ids = presupuestos.Select(p => p.IdPresupuesto).ToHashSet();
        var vigente = ids.Count == 0 ? Array.Empty<SaldoPartidaFila>()
            : await _consultas.VigenteAsync(new FiltroConsultaPresupuestaria(IdCentroCosto: query.IdCentroCosto,
                IdPresupuesto: query.IdPresupuesto));
        var rubros = vigente
            .Where(f => ids.Contains(f.IdPresupuesto))
            .GroupBy(f => (f.IdCatalogoPartida, f.CodigoPartida, f.NombrePartida))
            .OrderBy(g => g.Key.CodigoPartida, StringComparer.Ordinal)
            .Select(g => new RubroTablero(g.Key.IdCatalogoPartida, g.Key.CodigoPartida ?? string.Empty,
                g.Key.NombrePartida ?? string.Empty, g.Sum(f => f.MontoPresupuestado), g.Sum(f => f.MontoComprometido),
                g.Sum(f => f.MontoEjecutado)))
            .ToList();
        var ejecuciones = (await _consultas.EjecucionDiariaAsync(ids))
            .GroupBy(e => (e.Fecha.Date, e.IdCatalogoPartida))
            .Select(g => new EjecucionTablero(g.Key.Date, g.Key.IdCatalogoPartida, g.Sum(e => e.MontoEjecutado)))
            .ToList();

        var moneda = presupuestos.OrderBy(p => p.IdMoneda).FirstOrDefault();
        var encabezado = new EncabezadoTablero(centro.IdCentroCosto, centro.Codigo, centro.Nombre, centro.NombreProyecto,
            moneda?.IdMoneda, moneda?.CodigoMoneda, moneda?.SimboloMoneda, presupuestos.Select(p => p.IdMoneda).Distinct().Count(),
            presupuestos.Count, presupuestos.Min(p => p.FechaInicio), presupuestos.Max(p => p.FechaFin));
        return TableroPresupuestario.Construir(encabezado, rubros, ejecuciones, _reloj.GetLocalNow().DateTime);
    }
}
