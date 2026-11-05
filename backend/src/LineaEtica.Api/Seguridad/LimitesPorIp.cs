using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LineaEtica.Api.Seguridad;

public sealed class OpcionesLimites
{
    public const string Seccion = "LimitesIp";

    /// <summary>Intentos de ingreso (seguimiento y panel) por IP en cada ventana.</summary>
    public int Ingresos { get; set; } = 10;

    public int VentanaIngresosMinutos { get; set; } = 15;

    /// <summary>Denuncias nuevas por IP en cada ventana.</summary>
    public int Denuncias { get; set; } = 5;

    public int VentanaDenunciasMinutos { get; set; } = 60;
}

/// <summary>
/// Límites por IP contra fuerza bruta y abuso. La IP solo se usa en memoria como llave del contador:
/// no se guarda en la base ni se escribe en los registros.
/// </summary>
public static class LimitesPorIp
{
    public const string Ingreso = "Ingreso";
    public const string Denuncia = "Denuncia";

    public static IServiceCollection AgregarLimitesPorIp(this IServiceCollection servicios, OpcionesLimites opciones) =>
        servicios.AddRateLimiter(o =>
        {
            o.AddPolicy(Ingreso, contexto => Ventana(contexto, opciones.Ingresos, opciones.VentanaIngresosMinutos));
            o.AddPolicy(Denuncia, contexto => Ventana(contexto, opciones.Denuncias, opciones.VentanaDenunciasMinutos));
            o.OnRejected = async (contexto, ct) =>
            {
                var respuesta = contexto.HttpContext.Response;
                respuesta.StatusCode = StatusCodes.Status429TooManyRequests;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    respuesta.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString();
                }

                await respuesta.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Demasiados intentos",
                    Detail = "Hizo demasiadas solicitudes seguidas. Espere unos minutos e intente de nuevo.",
                }, options: null, contentType: "application/problem+json", ct);
            };
        });

    private static RateLimitPartition<string> Ventana(HttpContext contexto, int permitidos, int minutos) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, permitidos),
                Window = TimeSpan.FromMinutes(Math.Max(1, minutos)),
                QueueLimit = 0,
            });
}
