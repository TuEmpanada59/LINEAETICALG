using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>Acceso del denunciante a su caso con código y contraseña.</summary>
public sealed class SeguimientoService(LineaEticaDbContext db, CifradoDatos cifrado, TimeProvider reloj)
{
    public const int IntentosPermitidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Devuelve el caso si el código y la contraseña son correctos. Todas las fallas responden igual y tardan
    /// lo mismo, para no revelar si un código existe, está bloqueado o ya fue purgado.
    /// </summary>
    public async Task<Guid?> Ingresar(string? codigo, string? clave, CancellationToken ct = default)
    {
        var codigoLimpio = (codigo ?? "").Trim().ToUpperInvariant();
        var claveLimpia = (clave ?? "").Trim().ToUpperInvariant();
        var ahora = reloj.Ahora();

        var caso = await db.Denuncias.AsNoTracking()
            .Where(d => d.Codigo == codigoLimpio)
            .Select(d => new { d.Id, d.HashClave, d.BloqueadaHasta, d.DatosPersonalesEliminados })
            .SingleOrDefaultAsync(ct);

        if (caso is null || caso.DatosPersonalesEliminados || caso.HashClave is null)
        {
            Claves.SimularVerificacion(claveLimpia);
            return null;
        }

        if (caso.BloqueadaHasta > ahora)
        {
            Claves.SimularVerificacion(claveLimpia);
            db.Registrar(TipoActor.Denunciante, caso.Id, null, AccionesBitacora.IngresoBloqueado);
            await db.SaveChangesAsync(ct);
            return null;
        }

        var casoActual = db.Denuncias.Where(d => d.Id == caso.Id);

        if (!Claves.Verificar(claveLimpia, caso.HashClave))
        {
            // Actualizaciones atómicas: varios intentos simultáneos no pueden saltarse el conteo.
            await casoActual.ExecuteUpdateAsync(s => s.SetProperty(d => d.IntentosFallidos, d => d.IntentosFallidos + 1), ct);
            var bloqueos = await casoActual
                .Where(d => d.IntentosFallidos >= IntentosPermitidos)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.IntentosFallidos, 0)
                    .SetProperty(d => d.BloqueadaHasta, ahora + DuracionBloqueo), ct);

            db.Registrar(TipoActor.Denunciante, caso.Id, null,
                bloqueos > 0 ? AccionesBitacora.SeguimientoBloqueado : AccionesBitacora.IngresoFallido);
            await db.SaveChangesAsync(ct);
            return null;
        }

        await casoActual.ExecuteUpdateAsync(s => s
            .SetProperty(d => d.IntentosFallidos, 0)
            .SetProperty(d => d.BloqueadaHasta, (DateTime?)null), ct);
        db.Registrar(TipoActor.Denunciante, caso.Id, null, AccionesBitacora.IngresoSeguimiento);
        await db.SaveChangesAsync(ct);
        return caso.Id;
    }

    /// <summary>Estado del caso y buzón. Devuelve null si el caso ya no existe o fue purgado.</summary>
    public async Task<CasoDenunciante?> ObtenerCaso(Guid denunciaId, CancellationToken ct = default)
    {
        var caso = await db.Denuncias.AsNoTracking()
            .Where(d => d.Id == denunciaId && !d.DatosPersonalesEliminados)
            .Select(d => new
            {
                d.Codigo,
                d.Estado,
                d.CreadaEn,
                d.VenceRespuestaEn,
                d.CerradaEn,
                Mensajes = d.Mensajes
                    .OrderBy(m => m.CreadoEn).ThenBy(m => m.Id)
                    .Select(m => new { m.Autor, m.TextoCifrado, m.CreadoEn })
                    .ToList(),
            })
            .SingleOrDefaultAsync(ct);

        if (caso is null)
        {
            return null;
        }

        db.Registrar(TipoActor.Denunciante, denunciaId, null, AccionesBitacora.ConsultaDenunciante);
        await db.SaveChangesAsync(ct);

        var mensajes = caso.Mensajes
            .Select(m => new MensajeCaso(m.Autor, cifrado.Descifrar(m.TextoCifrado, ContextoCifrado.Mensaje(denunciaId)), m.CreadoEn))
            .ToList();

        return new CasoDenunciante(caso.Codigo, caso.Estado, caso.CreadaEn, caso.VenceRespuestaEn, caso.CerradaEn, mensajes);
    }

    /// <summary>El denunciante puede aportar información en cualquier momento, incluso con el caso cerrado.</summary>
    public async Task<bool> EnviarMensaje(Guid denunciaId, string? texto, CancellationToken ct = default)
    {
        var contenido = Validar.Requerido(texto, "mensaje", Limites.Mensaje);

        if (!await db.Denuncias.AnyAsync(d => d.Id == denunciaId && !d.DatosPersonalesEliminados, ct))
        {
            return false;
        }

        db.Mensajes.Add(new Mensaje
        {
            DenunciaId = denunciaId,
            Autor = AutorMensaje.Denunciante,
            TextoCifrado = cifrado.Cifrar(contenido, ContextoCifrado.Mensaje(denunciaId)),
            CreadoEn = reloj.Ahora(),
        });
        db.Registrar(TipoActor.Denunciante, denunciaId, null, AccionesBitacora.MensajeDenunciante);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
