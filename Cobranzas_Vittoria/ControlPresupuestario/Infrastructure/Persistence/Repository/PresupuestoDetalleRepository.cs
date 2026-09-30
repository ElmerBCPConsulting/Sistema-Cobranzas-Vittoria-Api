using System.Data;
using Cobranzas_Vittoria.Data;
using Cobranzas_Vittoria.Repositories;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Model;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Persistence;
using Cobranzas_Vittoria.ControlPresupuestario.Infrastructure.Persistence.Entity;
using Cobranzas_Vittoria.ControlPresupuestario.Infrastructure.Persistence.Mapper;
using Dapper;

namespace Cobranzas_Vittoria.ControlPresupuestario.Infrastructure.Persistence.Repository;

/// <summary>Adaptador Dapper de las partidas de una versión sobre los SPs usp_PresupuestoDetalle_*.</summary>
public sealed class PresupuestoDetalleRepository : RepositoryBase, IPresupuestoDetalleRepository
{
    private const string Schema = "ControlPresupuestario.";

    public PresupuestoDetalleRepository(IDbConnectionFactory factory) : base(factory) { }

    public Task<IReadOnlyList<PresupuestoDetalle>> ListarPorVersionAsync(int idPresupuestoVersion)
        => TraductorErroresSql.EjecutarAsync<IReadOnlyList<PresupuestoDetalle>>(async () =>
    {
        using var db = Open();
        var filas = await db.QueryAsync<PresupuestoDetalleEntity>(Schema + "usp_PresupuestoDetalle_ListarPorVersion",
            new { IdPresupuestoVersion = idPresupuestoVersion }, commandType: CommandType.StoredProcedure);
        return filas.Select(PresupuestoDetalleMapper.ToDomain).ToList();
    });

    public Task<int> AgregarAsync(PresupuestoDetalle d) => TraductorErroresSql.EjecutarAsync(async () =>
    {
        using var db = Open();
        return await db.ExecuteScalarAsync<int>(Schema + "usp_PresupuestoDetalle_Agregar",
            new { d.IdPresupuestoVersion, d.IdCatalogoPartida, d.MontoPresupuestado, d.Observacion },
            commandType: CommandType.StoredProcedure);
    });

    public Task ActualizarAsync(PresupuestoDetalle d) => TraductorErroresSql.EjecutarAsync(async () =>
    {
        using var db = Open();
        await db.ExecuteAsync(Schema + "usp_PresupuestoDetalle_Actualizar",
            new { d.IdPresupuestoDetalle, d.MontoPresupuestado, d.Observacion },
            commandType: CommandType.StoredProcedure);
    });

    public Task EliminarAsync(int idPresupuestoDetalle) => TraductorErroresSql.EjecutarAsync(async () =>
    {
        using var db = Open();
        await db.ExecuteAsync(Schema + "usp_PresupuestoDetalle_Eliminar",
            new { IdPresupuestoDetalle = idPresupuestoDetalle }, commandType: CommandType.StoredProcedure);
    });

    public Task<ResultadoCargaLote> CargarLoteAsync(LotePresupuestario lote) => TraductorErroresSql.EjecutarAsync(async () =>
    {
        // Mismo orden de columnas que ControlPresupuestario.TVP_PresupuestoDetalleLote.
        var tabla = new DataTable();
        tabla.Columns.Add("IdCatalogoPartida", typeof(int));
        tabla.Columns.Add("MontoPresupuestado", typeof(decimal));
        tabla.Columns.Add("Observacion", typeof(string));
        tabla.Columns.Add("_Fila", typeof(int));
        foreach (var item in lote.Items)
            tabla.Rows.Add(item.IdCatalogoPartida, item.MontoPresupuestado, (object?)item.Observacion ?? DBNull.Value, item.Fila);

        using var db = Open();
        var fila = await db.QueryFirstAsync<CargaLoteEntity>(Schema + "usp_PresupuestoDetalle_CargaLote",
            new
            {
                lote.IdPresupuestoVersion,
                Detalles = tabla.AsTableValuedParameter("ControlPresupuestario.TVP_PresupuestoDetalleLote"),
                lote.QuitarAusentes
            }, commandType: CommandType.StoredProcedure);
        return PresupuestoDetalleMapper.ToDomain(fila);
    });
}
