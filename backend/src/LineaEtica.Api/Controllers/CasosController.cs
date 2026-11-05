using LineaEtica.Api.Seguridad;
using LineaEtica.Domain;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineaEtica.Api.Controllers;

public sealed record NuevaAsignacion(int GestorId);

/// <summary>Casos en el panel. El servicio filtra lo que cada gestor puede ver.</summary>
[ApiController]
[Route("api/gestion/casos")]
[Authorize(Policy = Politicas.Gestor)]
public sealed class CasosController(CasosService casos) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CasoResumen>> Listar([FromQuery] EstadoDenuncia? estado, CancellationToken ct) =>
        casos.Listar(HttpContext.SesionActual(), estado, ct);

    [HttpGet("{id:guid}")]
    public Task<CasoGestion> Obtener(Guid id, CancellationToken ct) => casos.Obtener(HttpContext.SesionActual(), id, ct);

    [HttpPost("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, CambioEstado datos, CancellationToken ct)
    {
        await casos.CambiarEstado(HttpContext.SesionActual(), id, datos, ct);
        return NoContent();
    }

    [Authorize(Policy = Politicas.Oficial)]
    [HttpPost("{id:guid}/asignaciones")]
    public async Task<IActionResult> Asignar(Guid id, NuevaAsignacion datos, CancellationToken ct)
    {
        await casos.Asignar(HttpContext.SesionActual(), id, datos.GestorId, ct);
        return NoContent();
    }

    [Authorize(Policy = Politicas.Oficial)]
    [HttpDelete("{id:guid}/asignaciones/{gestorId:int}")]
    public async Task<IActionResult> Desasignar(Guid id, int gestorId, CancellationToken ct)
    {
        await casos.Desasignar(HttpContext.SesionActual(), id, gestorId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/mensajes")]
    public async Task<IActionResult> Responder(Guid id, NuevoMensaje datos, CancellationToken ct)
    {
        await casos.Responder(HttpContext.SesionActual(), id, datos.Texto, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/anotaciones")]
    public async Task<IActionResult> Anotar(Guid id, NuevoMensaje datos, CancellationToken ct)
    {
        await casos.Anotar(HttpContext.SesionActual(), id, datos.Texto, ct);
        return NoContent();
    }
}
