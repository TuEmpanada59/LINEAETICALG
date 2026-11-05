using System.Globalization;
using System.Security.Claims;
using LineaEtica.Domain;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LineaEtica.Api.Seguridad;

public static class Esquemas
{
    public const string Seguimiento = "Seguimiento";
    public const string Gestion = "Gestion";
}

public static class Politicas
{
    public const string Denunciante = "Denunciante";

    /// <summary>Cualquier sesión de gestor, incluso incompleta: permite cambiar la clave y registrar el TOTP.</summary>
    public const string GestorInicial = "GestorInicial";

    /// <summary>Sesión completa: contraseña definitiva y segundo factor registrado.</summary>
    public const string Gestor = "Gestor";

    public const string Oficial = "Oficial";
}

public static class Reclamos
{
    public const string Denuncia = "denuncia";
    public const string Gestor = "gestor";
    public const string Sello = "sello";
    public const string Rol = "rol";
    public const string Etapa = "etapa";

    public const string EtapaCompleta = "completa";
    public const string EtapaInicial = "inicial";
}

/// <summary>Cookie del panel: solo guarda el id y el sello; rol y etapa se leen de la base en cada solicitud.</summary>
public static class SesionesGestion
{
    public static ClaimsPrincipal Principal(SesionGestor sesion) =>
        new(new ClaimsIdentity(
            [
                new Claim(Reclamos.Gestor, sesion.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(Reclamos.Sello, sesion.Sello.ToString()),
                new Claim(Reclamos.Rol, sesion.Rol.ToString()),
                new Claim(Reclamos.Etapa, sesion.Completa ? Reclamos.EtapaCompleta : Reclamos.EtapaInicial),
            ],
            Esquemas.Gestion));

    /// <summary>
    /// Rechaza la cookie si el gestor fue desactivado o le restablecieron la clave o el TOTP (cambia el sello),
    /// y refresca rol y etapa con lo que diga la base.
    /// </summary>
    public static async Task Validar(CookieValidatePrincipalContext contexto)
    {
        var principal = contexto.Principal;
        if (!int.TryParse(principal?.FindFirstValue(Reclamos.Gestor), CultureInfo.InvariantCulture, out var gestorId) ||
            !Guid.TryParse(principal?.FindFirstValue(Reclamos.Sello), out var sello))
        {
            contexto.RejectPrincipal();
            return;
        }

        var acceso = contexto.HttpContext.RequestServices.GetRequiredService<AccesoGestorService>();
        var sesion = await acceso.ObtenerSesion(gestorId, sello, contexto.HttpContext.RequestAborted);
        if (sesion is null)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(Esquemas.Gestion);
            return;
        }

        contexto.ReplacePrincipal(Principal(sesion));
        contexto.HttpContext.Items[typeof(SesionGestor)] = sesion;
    }

    /// <summary>Sesión ya validada en esta solicitud.</summary>
    public static SesionGestor SesionActual(this HttpContext contexto) =>
        contexto.Items[typeof(SesionGestor)] as SesionGestor
        ?? throw new InvalidOperationException("La solicitud no tiene una sesión de gestor validada.");
}
