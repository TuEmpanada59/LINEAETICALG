using LineaEtica.Api.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineaEtica.Api.Controllers;

/// <summary>Pantalla mínima de gestores. Solo el oficial de cumplimiento.</summary>
[ApiController]
[Route("api/gestion/gestores")]
[Authorize(Policy = Politicas.Oficial)]
public sealed class GestoresController(GestoresService gestores) : ControllerBase
{
    [HttpGet]
    public Task<List<GestorResumen>> Listar(CancellationToken ct) => gestores.Listar(ct);

    [HttpPost]
    public Task<ClaveTemporal> Crear(NuevoGestor datos, CancellationToken ct) => gestores.Crear(HttpContext.SesionActual(), datos, ct);

    [HttpPost("{id:int}/restablecer-clave")]
    public Task<ClaveTemporal> RestablecerClave(int id, CancellationToken ct) =>
        gestores.RestablecerClave(HttpContext.SesionActual(), id, ct);

    [HttpPost("{id:int}/restablecer-totp")]
    public async Task<IActionResult> RestablecerTotp(int id, CancellationToken ct)
    {
        await gestores.RestablecerTotp(HttpContext.SesionActual(), id, ct);
        return NoContent();
    }
}
