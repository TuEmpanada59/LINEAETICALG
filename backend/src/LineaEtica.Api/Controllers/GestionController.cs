using LineaEtica.Api.Seguridad;
using LineaEtica.Domain;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LineaEtica.Api.Controllers;

public sealed record IngresoGestor(string? Correo, string? Clave, string? Codigo);

public sealed record CambioClave(string? ClaveActual, string? ClaveNueva);

public sealed record CodigoTotp(string? Codigo);

/// <summary>Lo que el panel necesita saber de la sesión para decidir qué pantalla mostrar.</summary>
public sealed record EstadoSesion(string Nombre, string Correo, RolGestor Rol, bool DebeCambiarClave, bool TotpConfirmado, bool Completa)
{
    public static EstadoSesion De(SesionGestor s) => new(s.Nombre, s.Correo, s.Rol, s.DebeCambiarClave, s.TotpConfirmado, s.Completa);
}

/// <summary>Sesión de los gestores del panel.</summary>
[ApiController]
[Route("api/gestion")]
public sealed class GestionController(AccesoGestorService acceso) : ControllerBase
{
    [EnableRateLimiting(LimitesPorIp.Ingreso)]
    [HttpPost("ingresar")]
    public async Task<IActionResult> Ingresar(IngresoGestor datos, CancellationToken ct)
    {
        var sesion = await acceso.Ingresar(datos.Correo, datos.Clave, datos.Codigo, ct);
        if (sesion is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No fue posible ingresar",
                detail: "El correo, la contraseña o el código no son correctos, o la cuenta está bloqueada temporalmente.");
        }

        return await Iniciar(sesion);
    }

    [HttpPost("salir")]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(Esquemas.Gestion);
        return NoContent();
    }

    [Authorize(Policy = Politicas.GestorInicial)]
    [HttpGet("sesion")]
    public EstadoSesion Sesion() => EstadoSesion.De(HttpContext.SesionActual());

    [Authorize(Policy = Politicas.GestorInicial)]
    [HttpPost("clave")]
    public async Task<IActionResult> CambiarClave(CambioClave datos, CancellationToken ct) =>
        await Iniciar(await acceso.CambiarClave(HttpContext.SesionActual().Id, datos.ClaveActual, datos.ClaveNueva, ct));

    [Authorize(Policy = Politicas.GestorInicial)]
    [HttpPost("totp/registro")]
    public Task<RegistroTotp> RegistrarTotp(CancellationToken ct) => acceso.IniciarRegistroTotp(HttpContext.SesionActual().Id, ct);

    [Authorize(Policy = Politicas.GestorInicial)]
    [HttpPost("totp/confirmar")]
    public async Task<IActionResult> ConfirmarTotp(CodigoTotp datos, CancellationToken ct) =>
        await Iniciar(await acceso.ConfirmarTotp(HttpContext.SesionActual().Id, datos.Codigo, ct));

    /// <summary>Emite (o reemplaza) la cookie con el sello y la etapa vigentes.</summary>
    private async Task<IActionResult> Iniciar(SesionGestor sesion)
    {
        await HttpContext.SignInAsync(Esquemas.Gestion, SesionesGestion.Principal(sesion));
        return Ok(EstadoSesion.De(sesion));
    }
}
