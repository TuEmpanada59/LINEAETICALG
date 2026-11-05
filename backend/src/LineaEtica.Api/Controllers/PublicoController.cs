using System.Security.Claims;
using LineaEtica.Api.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineaEtica.Api.Controllers;

public sealed record IngresoSeguimiento(string? Codigo, string? Clave);

public sealed record NuevoMensaje(string? Texto);

/// <summary>Parte pública del canal: no requiere cuenta.</summary>
[ApiController]
[Route("api/publico")]
public sealed class PublicoController(DenunciaService denuncias, SeguimientoService seguimiento) : ControllerBase
{
    [HttpGet("formulario")]
    public Task<Formulario> Formulario(CancellationToken ct) => denuncias.ObtenerFormulario(ct);

    [HttpPost("denuncias")]
    public Task<DenunciaRecibida> Denunciar(NuevaDenuncia datos, CancellationToken ct) => denuncias.Crear(datos, ct);

    [HttpPost("seguimiento/ingresar")]
    public async Task<IActionResult> Ingresar(IngresoSeguimiento datos, CancellationToken ct)
    {
        var denunciaId = await seguimiento.Ingresar(datos.Codigo, datos.Clave, ct);
        if (denunciaId is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No fue posible ingresar",
                detail: "El código o la contraseña no son correctos, o hubo demasiados intentos. Intente de nuevo más tarde.");
        }

        var identidad = new ClaimsIdentity([new Claim(Reclamos.Denuncia, denunciaId.Value.ToString())], Esquemas.Seguimiento);
        await HttpContext.SignInAsync(Esquemas.Seguimiento, new ClaimsPrincipal(identidad));
        return NoContent();
    }

    [Authorize(Policy = Politicas.Denunciante)]
    [HttpGet("seguimiento")]
    public async Task<IActionResult> Caso(CancellationToken ct)
    {
        var caso = await seguimiento.ObtenerCaso(DenunciaActual(), ct);
        return caso is null ? await SesionTerminada() : Ok(caso);
    }

    [Authorize(Policy = Politicas.Denunciante)]
    [HttpPost("seguimiento/mensajes")]
    public async Task<IActionResult> Mensaje(NuevoMensaje datos, CancellationToken ct) =>
        await seguimiento.EnviarMensaje(DenunciaActual(), datos.Texto, ct) ? NoContent() : await SesionTerminada();

    [HttpPost("seguimiento/salir")]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(Esquemas.Seguimiento);
        return NoContent();
    }

    private Guid DenunciaActual() => Guid.Parse(User.FindFirstValue(Reclamos.Denuncia)!);

    private async Task<IActionResult> SesionTerminada()
    {
        await HttpContext.SignOutAsync(Esquemas.Seguimiento);
        return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sesión terminada",
            detail: "El caso ya no está disponible. Vuelva a ingresar.");
    }
}
