using Cobranzas_Vittoria.Seguridad.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Cobranzas_Vittoria.Application.Common.Exports;
using Cobranzas_Vittoria.ControlPresupuestario.Application.Common;
using Cobranzas_Vittoria.ControlPresupuestario.Application.MovimientoPresupuestal.ListarPorDetalle;
using Cobranzas_Vittoria.ControlPresupuestario.Application.MovimientoPresupuestal.Obtener;
using Cobranzas_Vittoria.ControlPresupuestario.Application.Presupuesto.Actualizar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.Presupuesto.Crear;
using Cobranzas_Vittoria.ControlPresupuestario.Application.Presupuesto.Listar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.Presupuesto.Obtener;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.Actualizar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.Agregar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.CargarLote;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.Eliminar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.Importar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.ListarPorVersion;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.Plantilla;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoDetalle.RegistrarAjuste;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoVersion.Anular;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoVersion.Aprobar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoVersion.CrearNueva;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoVersion.Listar;
using Cobranzas_Vittoria.ControlPresupuestario.Application.PresupuestoVersion.Obtener;
using Cobranzas_Vittoria.ControlPresupuestario.Presentation.Dto.MovimientoPresupuestal;
using Cobranzas_Vittoria.ControlPresupuestario.Presentation.Dto.Presupuesto;
using Cobranzas_Vittoria.ControlPresupuestario.Presentation.Dto.PresupuestoDetalle;
using Cobranzas_Vittoria.ControlPresupuestario.Presentation.Dto.PresupuestoVersion;
using Cobranzas_Vittoria.ControlPresupuestario.Presentation.Http;

namespace Cobranzas_Vittoria.ControlPresupuestario.Presentation.Controller;

/// <summary>
/// Presupuestos y sus recursos subordinados: versiones, partidas de cada versión y su ledger de movimientos.
/// Aprobar y anular son acciones de negocio explícitas, no un PUT genérico de estado.
/// </summary>
[ApiController]
[Route("api/control-presupuestario")]
public sealed class PresupuestoController : ControllerBase
{
    private const long MaxRequestSize = 11_000_000;

    // -------------------------------------------------------------- presupuestos

    [HttpGet("presupuestos")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Ver)]
    public async Task<IActionResult> Listar([FromServices] ListarPresupuestoHandler handler, [FromQuery] bool? activo,
        [FromQuery] int? idCentroCosto, [FromQuery] int? idMoneda, [FromQuery] string? busqueda)
    {
        var presupuestos = await handler.HandleAsync(new ListarPresupuestoQuery(activo, idCentroCosto, idMoneda, busqueda));
        return Ok(presupuestos.Select(PresupuestoResponse.Desde));
    }

    [HttpGet("presupuestos/{id:int}", Name = "ObtenerPresupuesto")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Ver)]
    public async Task<IActionResult> Obtener([FromServices] ObtenerPresupuestoHandler handler, int id)
        => Ok(PresupuestoResponse.Desde(await handler.HandleAsync(new ObtenerPresupuestoQuery(id))));

    /// <summary>Crea el presupuesto y, en la misma transacción, su versión 1 en BORRADOR.</summary>
    [HttpPost("presupuestos")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Crear)]
    public async Task<IActionResult> Crear([FromServices] CrearPresupuestoHandler handler, [FromBody] CrearPresupuestoRequest r)
    {
        var creado = await handler.HandleAsync(new CrearPresupuestoCommand(r.IdCentroCosto, r.IdMoneda, r.Codigo, r.Nombre,
            r.Descripcion, r.FechaInicio, r.FechaFin));
        return CreatedAtRoute("ObtenerPresupuesto", new { id = creado.IdPresupuesto }, CrearPresupuestoResponse.Desde(creado));
    }

    [HttpPut("presupuestos/{id:int}")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Actualizar)]
    public async Task<IActionResult> Actualizar([FromServices] ActualizarPresupuestoHandler handler, int id,
        [FromBody] ActualizarPresupuestoRequest r)
        => Ok(PresupuestoResponse.Desde(await handler.HandleAsync(new ActualizarPresupuestoCommand(id, r.Nombre, r.Activo,
            r.Descripcion, r.FechaInicio, r.FechaFin))));

    // ------------------------------------------------------------------ versiones

    [HttpGet("presupuestos/{id:int}/versiones")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Ver)]
    public async Task<IActionResult> ListarVersiones([FromServices] ListarPresupuestoVersionHandler handler, int id)
        => Ok((await handler.HandleAsync(new ListarPresupuestoVersionQuery(id))).Select(PresupuestoVersionResponse.Desde));

    /// <summary>Crea una versión nueva copiando el snapshot de la versión APROBADA vigente.</summary>
    [HttpPost("presupuestos/{id:int}/versiones")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Version.Crear)]
    public async Task<IActionResult> CrearVersion([FromServices] CrearNuevaVersionHandler handler, int id,
        [FromBody] CrearVersionRequest? r)
    {
        var creada = await handler.HandleAsync(new CrearNuevaVersionCommand(id, r?.Descripcion, r?.MotivoCambio));
        return CreatedAtRoute("ObtenerVersion", new { id, versionId = creada.IdPresupuestoVersion }, CrearVersionResponse.Desde(creada));
    }

    [HttpGet("presupuestos/{id:int}/versiones/{versionId:int}", Name = "ObtenerVersion")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Ver)]
    public async Task<IActionResult> ObtenerVersion([FromServices] ObtenerPresupuestoVersionHandler handler, int id, int versionId)
        => Ok(PresupuestoVersionResponse.Desde(await handler.HandleAsync(new ObtenerPresupuestoVersionQuery(id, versionId))));

    [HttpPost("presupuestos/{id:int}/versiones/{versionId:int}/aprobar")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Version.Aprobar)]
    public async Task<IActionResult> AprobarVersion([FromServices] AprobarVersionHandler handler, int id, int versionId)
        => Ok(PresupuestoVersionResponse.Desde(await handler.HandleAsync(new AprobarVersionCommand(id, versionId))));

    [HttpPost("presupuestos/{id:int}/versiones/{versionId:int}/anular")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Version.Anular)]
    public async Task<IActionResult> AnularVersion([FromServices] AnularVersionHandler handler, int id, int versionId,
        [FromBody] AnularVersionRequest r)
        => Ok(PresupuestoVersionResponse.Desde(await handler.HandleAsync(new AnularVersionCommand(id, versionId, r.Motivo))));

    // ----------------------------------------------------------- partidas de versión

    [HttpGet("presupuestos/{id:int}/versiones/{versionId:int}/partidas")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.Ver)]
    public async Task<IActionResult> ListarPartidas([FromServices] ListarPresupuestoDetalleHandler handler, int id, int versionId)
        => Ok((await handler.HandleAsync(new ListarPresupuestoDetalleQuery(id, versionId))).Select(PartidaVersionResponse.Desde));

    [HttpPost("presupuestos/{id:int}/versiones/{versionId:int}/partidas")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    public async Task<IActionResult> AgregarPartida([FromServices] AgregarPresupuestoDetalleHandler handler, int id,
        int versionId, [FromBody] AgregarPartidaRequest r)
    {
        var detalle = await handler.HandleAsync(new AgregarPresupuestoDetalleCommand(id, versionId, r.IdCatalogoPartida,
            r.MontoPresupuestado, r.Observacion));
        return Created($"/api/control-presupuestario/presupuestos/{id}/versiones/{versionId}/partidas/{detalle.IdPresupuestoDetalle}",
            PartidaVersionResponse.Desde(detalle));
    }

    /// <summary>Carga completa de montos sobre una versión BORRADOR (todo o nada).</summary>
    [HttpPut("presupuestos/{id:int}/versiones/{versionId:int}/partidas/lote")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    public async Task<IActionResult> CargarLote([FromServices] CargarLoteHandler handler, int id, int versionId,
        [FromBody] CargaLoteRequest r)
    {
        var items = r.Detalles.Select(d => new CargarLoteItem(d.IdCatalogoPartida, d.MontoPresupuestado, d.Observacion)).ToList();
        return Ok(CargaLoteResponse.Desde(await handler.HandleAsync(new CargarLoteCommand(id, versionId, items, r.QuitarAusentes))));
    }

    /// <summary>Carga los montos de la versión desde CSV/XLSX. En 422 el cuerpo trae errores[] por fila.</summary>
    [HttpPost("presupuestos/{id:int}/versiones/{versionId:int}/partidas/importar")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    [RequestSizeLimit(MaxRequestSize)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportarPartidas([FromServices] ImportarPresupuestoDetalleHandler handler, int id,
        int versionId, IFormFile? archivo, [FromForm] bool quitarAusentes, CancellationToken ct)
    {
        var archivoTabular = await ArchivoSubido.LeerAsync(archivo, ct);
        var resultado = await handler.HandleAsync(new ImportarPresupuestoDetalleCommand(id, versionId, archivoTabular, quitarAusentes));
        return Ok(CargaLoteResponse.Desde(resultado));
    }

    /// <summary>Plantilla con todas las partidas hoja activas y los montos actuales de la versión.</summary>
    [HttpGet("presupuestos/{id:int}/versiones/{versionId:int}/partidas/plantilla")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    public async Task<IActionResult> PlantillaPartidas([FromServices] ObtenerPlantillaPresupuestoHandler handler,
        [FromServices] IExcelExporter excel, int id, int versionId, [FromQuery] string? formato = "csv")
    {
        var tipo = PlantillaArchivo.NormalizarFormato(formato);
        var filas = await handler.HandleAsync(new ObtenerPlantillaPresupuestoQuery(id, versionId));
        var nombre = PlantillaArchivo.NombreArchivo($"plantilla-presupuesto-version-{versionId}", tipo);
        if (tipo == "csv")
            return File(PlantillaArchivo.Csv(ColumnasImportacion.PresupuestoDetalle, filas.Select(f => new[]
            {
                f.CodigoPartida, f.Partida, f.Monto.ToString("0.00", CultureInfo.InvariantCulture), f.Observacion ?? string.Empty
            })), PlantillaArchivo.TipoCsv, nombre);
        var xlsx = excel.ExportToXlsx(filas.Select(f => new PresupuestoPlantillaFila
        {
            CodigoPartida = f.CodigoPartida,
            Partida = f.Partida,
            Monto = f.Monto,
            Observacion = f.Observacion ?? string.Empty
        }).ToList(), new ExcelSheetConfig
        {
            SheetName = "Presupuesto",
            Title = "Plantilla de carga de presupuesto",
            FiltersSubtitle = "Complete la columna Monto (0 si la partida no aplica). La columna Partida es informativa.",
            GeneratedAtSubtitle = "Generado el: {0}",
            IncludeTotalsRow = false,
            HeaderRowIndex = 0
        });
        return File(xlsx, PlantillaArchivo.TipoXlsx, nombre);
    }

    [HttpPut("presupuestos/{id:int}/versiones/{versionId:int}/partidas/{detalleId:int}")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    public async Task<IActionResult> ActualizarPartida([FromServices] ActualizarPresupuestoDetalleHandler handler, int id,
        int versionId, int detalleId, [FromBody] ActualizarPartidaRequest r)
        => Ok(PartidaVersionResponse.Desde(await handler.HandleAsync(new ActualizarPresupuestoDetalleCommand(id, versionId,
            detalleId, r.MontoPresupuestado, r.Observacion))));

    [HttpDelete("presupuestos/{id:int}/versiones/{versionId:int}/partidas/{detalleId:int}")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.EditarDetalle)]
    public async Task<IActionResult> EliminarPartida([FromServices] EliminarPresupuestoDetalleHandler handler, int id,
        int versionId, int detalleId)
    {
        await handler.HandleAsync(new EliminarPresupuestoDetalleCommand(id, versionId, detalleId));
        return NoContent();
    }

    // ---------------------------------------------------------------- movimientos

    [HttpGet("presupuestos/{id:int}/versiones/{versionId:int}/partidas/{detalleId:int}/movimientos")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Movimiento.Ver)]
    public async Task<IActionResult> ListarMovimientos([FromServices] ListarMovimientosPorDetalleHandler handler, int id,
        int versionId, int detalleId)
        => Ok((await handler.HandleAsync(new ListarMovimientosPorDetalleQuery(id, versionId, detalleId)))
            .Select(MovimientoPresupuestalResponse.Desde));

    /// <summary>Ajuste manual: la única escritura admitida sobre el ledger, como acción de negocio explícita.</summary>
    [HttpPost("presupuestos/{id:int}/versiones/{versionId:int}/partidas/{detalleId:int}/ajustes")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Presupuesto.RegistrarAjuste)]
    public async Task<IActionResult> RegistrarAjuste([FromServices] RegistrarAjusteHandler handler, int id, int versionId,
        int detalleId, [FromBody] RegistrarAjusteRequest r)
    {
        var movimiento = await handler.HandleAsync(new RegistrarAjusteCommand(id, versionId, detalleId, r.Afectacion,
            r.Direccion, r.Monto, r.Observacion, r.Fecha));
        return CreatedAtRoute("ObtenerMovimiento", new { idMovimiento = movimiento.IdMovimientoPresupuestal },
            MovimientoPresupuestalResponse.Desde(movimiento));
    }

    [HttpGet("movimientos/{idMovimiento:long}", Name = "ObtenerMovimiento")]
    [AuthorizePermission(Permisos.ControlPresupuestario.Movimiento.Ver)]
    public async Task<IActionResult> ObtenerMovimiento([FromServices] ObtenerMovimientoHandler handler, long idMovimiento)
        => Ok(MovimientoPresupuestalResponse.Desde(await handler.HandleAsync(new ObtenerMovimientoQuery(idMovimiento))));
}
