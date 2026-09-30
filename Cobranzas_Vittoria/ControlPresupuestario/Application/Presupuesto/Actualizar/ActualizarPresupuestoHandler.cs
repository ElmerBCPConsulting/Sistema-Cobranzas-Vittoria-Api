using Cobranzas_Vittoria.ControlPresupuestario.Application.Common;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Excepciones;
using Cobranzas_Vittoria.ControlPresupuestario.Domain.Persistence;

namespace Cobranzas_Vittoria.ControlPresupuestario.Application.Presupuesto.Actualizar;

public sealed class ActualizarPresupuestoHandler
{
    private readonly IPresupuestoRepository _repository;
    private readonly ILogger<ActualizarPresupuestoHandler> _logger;

    public ActualizarPresupuestoHandler(IPresupuestoRepository repository, ILogger<ActualizarPresupuestoHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PresupuestoResult> HandleAsync(ActualizarPresupuestoCommand command)
    {
        PresupuestoValidator.ValidarId(command.IdPresupuesto);
        var presupuesto = await _repository.ObtenerAsync(command.IdPresupuesto)
            ?? throw new PresupuestoNoEncontradoException(command.IdPresupuesto);
        presupuesto.Actualizar(command.Nombre, command.Activo, command.Descripcion, command.FechaInicio, command.FechaFin);
        await _repository.ActualizarAsync(presupuesto);
        _logger.LogInformation("Presupuesto actualizado: IdPresupuesto={Id}", command.IdPresupuesto);
        var actualizado = await _repository.ObtenerAsync(command.IdPresupuesto)
            ?? throw new PresupuestoNoEncontradoException(command.IdPresupuesto);
        return PresupuestoResult.Desde(actualizado);
    }
}
