using LineaEtica.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace LineaEtica.Api;

/// <summary>
/// Validación (400), no encontrado (404) y conflicto (409) con su mensaje. Cualquier otro: 500 genérico,
/// sin detalles técnicos para el usuario.
/// </summary>
internal sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken ct)
    {
        var (estado, titulo, detalle) = error switch
        {
            ErrorValidacion validacion => (StatusCodes.Status400BadRequest, "Datos no válidos", validacion.Message),
            ErrorNoEncontrado noEncontrado => (StatusCodes.Status404NotFound, "No encontrado", noEncontrado.Message),
            ErrorConflicto conflicto => (StatusCodes.Status409Conflict, "Conflicto", conflicto.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error", "Ocurrió un error inesperado. Intente de nuevo más tarde."),
        };

        if (estado == StatusCodes.Status500InternalServerError)
        {
            registro.LogError(error, "Error no controlado en {Ruta}", contexto.Request.Path);
        }

        contexto.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = { Status = estado, Title = titulo, Detail = detalle },
        });
    }
}
