using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>Ingreso de gestores: contraseña, segundo factor (TOTP) obligatorio y cambio de la clave temporal.</summary>
public sealed class AccesoGestorService(LineaEticaDbContext db, CifradoDatos cifrado, TimeProvider reloj)
{
    public const int IntentosPermitidos = 5;
    public const int LongitudMinimaClave = 12;
    public const int LongitudMaximaClave = 128;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Devuelve la sesión si los datos son correctos. Con el segundo factor ya registrado, el código es obligatorio;
    /// sin él (primer ingreso o tras un restablecimiento), la contraseña abre una sesión incompleta para registrarlo.
    /// Todas las fallas responden igual.
    /// </summary>
    public async Task<SesionGestor?> Ingresar(string? correo, string? clave, string? codigo, CancellationToken ct = default)
    {
        var correoLimpio = (correo ?? "").Trim().ToLowerInvariant();
        var claveRecibida = clave ?? "";
        var ahora = reloj.Ahora();

        var gestor = await db.Gestores.AsNoTracking().SingleOrDefaultAsync(g => g.Correo == correoLimpio, ct);
        if (gestor is null)
        {
            Claves.SimularVerificacion(claveRecibida);
            return null;
        }

        if (!gestor.Activo || gestor.BloqueadoHasta > ahora)
        {
            Claves.SimularVerificacion(claveRecibida);
            db.Registrar(TipoActor.Gestor, null, gestor.Id, AccionesBitacora.IngresoGestorRechazado);
            await db.SaveChangesAsync(ct);
            return null;
        }

        var gestorActual = db.Gestores.Where(g => g.Id == gestor.Id);

        var valido = Claves.Verificar(claveRecibida, gestor.HashClave);
        if (valido && gestor.TotpConfirmadoEn is not null)
        {
            var secreto = Convert.FromBase64String(
                cifrado.Descifrar(gestor.TotpSecretoCifrado!, ContextoCifrado.SecretoTotp(gestor.Id)));
            var paso = Totp.Verificar(secreto, codigo?.Trim(), reloj.GetUtcNow(), gestor.TotpUltimoPaso);

            // Guarda el paso de forma atómica: el mismo código no sirve dos veces, ni en solicitudes simultáneas.
            valido = paso is not null && await gestorActual
                .Where(g => g.TotpUltimoPaso == null || g.TotpUltimoPaso < paso)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.TotpUltimoPaso, paso), ct) > 0;
        }

        if (!valido)
        {
            await gestorActual.ExecuteUpdateAsync(s => s.SetProperty(g => g.IntentosFallidos, g => g.IntentosFallidos + 1), ct);
            var bloqueos = await gestorActual
                .Where(g => g.IntentosFallidos >= IntentosPermitidos)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(g => g.IntentosFallidos, 0)
                    .SetProperty(g => g.BloqueadoHasta, ahora + DuracionBloqueo), ct);

            db.Registrar(TipoActor.Gestor, null, gestor.Id,
                bloqueos > 0 ? AccionesBitacora.GestorBloqueado : AccionesBitacora.IngresoGestorFallido);
            await db.SaveChangesAsync(ct);
            return null;
        }

        await gestorActual.ExecuteUpdateAsync(s => s
            .SetProperty(g => g.IntentosFallidos, 0)
            .SetProperty(g => g.BloqueadoHasta, (DateTime?)null), ct);
        db.Registrar(TipoActor.Gestor, null, gestor.Id, AccionesBitacora.IngresoGestor);
        await db.SaveChangesAsync(ct);
        return Sesion(gestor);
    }

    /// <summary>Sesión vigente, o null si el gestor fue desactivado o su sello cambió (clave o TOTP restablecidos).</summary>
    public async Task<SesionGestor?> ObtenerSesion(int gestorId, Guid sello, CancellationToken ct = default)
    {
        var gestor = await db.Gestores.AsNoTracking()
            .SingleOrDefaultAsync(g => g.Id == gestorId && g.Activo && g.SelloSesion == sello, ct);
        return gestor is null ? null : Sesion(gestor);
    }

    /// <summary>Cambia la contraseña y renueva el sello: las demás sesiones del gestor quedan cerradas.</summary>
    public async Task<SesionGestor> CambiarClave(int gestorId, string? claveActual, string? claveNueva, CancellationToken ct = default)
    {
        var gestor = await db.Gestores.SingleAsync(g => g.Id == gestorId, ct);

        if (!Claves.Verificar(claveActual ?? "", gestor.HashClave))
        {
            throw new ErrorValidacion("La contraseña actual no es correcta.");
        }

        var nueva = claveNueva ?? "";
        if (nueva.Length is < LongitudMinimaClave or > LongitudMaximaClave)
        {
            throw new ErrorValidacion(
                $"La contraseña nueva debe tener entre {LongitudMinimaClave} y {LongitudMaximaClave} caracteres.");
        }

        if (nueva == claveActual)
        {
            throw new ErrorValidacion("La contraseña nueva debe ser distinta de la actual.");
        }

        gestor.HashClave = Claves.Hash(nueva);
        gestor.DebeCambiarClave = false;
        gestor.SelloSesion = Guid.NewGuid();
        db.Registrar(TipoActor.Gestor, null, gestorId, AccionesBitacora.CambioClave);
        await db.SaveChangesAsync(ct);
        return Sesion(gestor);
    }

    /// <summary>Genera un secreto nuevo mientras el segundo factor no esté confirmado. Cada llamada reemplaza el anterior.</summary>
    public async Task<RegistroTotp> IniciarRegistroTotp(int gestorId, CancellationToken ct = default)
    {
        var gestor = await db.Gestores.SingleAsync(g => g.Id == gestorId, ct);
        if (gestor.TotpConfirmadoEn is not null)
        {
            throw new ErrorValidacion("El segundo factor ya está registrado. Pida al oficial que lo restablezca si lo perdió.");
        }

        var secreto = Totp.GenerarSecreto();
        gestor.TotpSecretoCifrado = cifrado.Cifrar(Convert.ToBase64String(secreto), ContextoCifrado.SecretoTotp(gestorId));
        await db.SaveChangesAsync(ct);
        return new RegistroTotp(Totp.UriDeRegistro(secreto, gestor.Correo), Totp.ABase32(secreto));
    }

    public async Task<SesionGestor> ConfirmarTotp(int gestorId, string? codigo, CancellationToken ct = default)
    {
        var gestor = await db.Gestores.SingleAsync(g => g.Id == gestorId, ct);
        if (gestor.TotpConfirmadoEn is not null)
        {
            throw new ErrorValidacion("El segundo factor ya está registrado.");
        }

        if (gestor.TotpSecretoCifrado is null)
        {
            throw new ErrorValidacion("Primero inicie el registro del segundo factor.");
        }

        var secreto = Convert.FromBase64String(cifrado.Descifrar(gestor.TotpSecretoCifrado, ContextoCifrado.SecretoTotp(gestorId)));
        var paso = Totp.Verificar(secreto, codigo?.Trim(), reloj.GetUtcNow(), gestor.TotpUltimoPaso)
                   ?? throw new ErrorValidacion("El código no es correcto. Revise la hora del celular e intente de nuevo.");

        gestor.TotpConfirmadoEn = reloj.Ahora();
        gestor.TotpUltimoPaso = paso;
        db.Registrar(TipoActor.Gestor, null, gestorId, AccionesBitacora.RegistroTotp);
        await db.SaveChangesAsync(ct);
        return Sesion(gestor);
    }

    private static SesionGestor Sesion(Gestor g) =>
        new(g.Id, g.Nombre, g.Correo, g.Rol, g.SelloSesion, g.DebeCambiarClave, g.TotpConfirmadoEn is not null);
}
