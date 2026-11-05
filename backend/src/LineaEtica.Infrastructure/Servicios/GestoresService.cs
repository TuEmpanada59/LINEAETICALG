using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>Pantalla mínima de gestores (solo el oficial): crear y restablecer contraseña o segundo factor.</summary>
public sealed class GestoresService(LineaEticaDbContext db, TimeProvider reloj)
{
    public Task<List<GestorResumen>> Listar(CancellationToken ct = default) =>
        db.Gestores.AsNoTracking()
            .OrderBy(g => g.Nombre)
            .Select(g => new GestorResumen(g.Id, g.Nombre, g.Correo, g.Rol, g.Activo, g.TotpConfirmadoEn != null,
                g.DebeCambiarClave, g.BloqueadoHasta))
            .ToListAsync(ct);

    public async Task<ClaveTemporal> Crear(SesionGestor oficial, NuevoGestor datos, CancellationToken ct = default)
    {
        var nombre = Validar.Requerido(datos.Nombre, "nombre", Limites.Nombre);
        var correo = Validar.Correo(datos.Correo?.Trim().ToLowerInvariant());
        var rol = Validar.Opcion(datos.Rol, "rol");

        if (await db.Gestores.AnyAsync(g => g.Correo == correo, ct))
        {
            throw new ErrorValidacion("Ya existe un gestor con ese correo.");
        }

        var clave = Claves.GenerarClaveTemporal();
        var gestor = new Gestor
        {
            Nombre = nombre,
            Correo = correo,
            HashClave = Claves.Hash(clave),
            Rol = rol,
            CreadoEn = reloj.Ahora(),
            SelloSesion = Guid.NewGuid(),
        };
        db.Gestores.Add(gestor);
        db.Registrar(TipoActor.Gestor, null, oficial.Id, AccionesBitacora.GestorCreado, $"{nombre} <{correo}> ({rol})");
        await db.SaveChangesAsync(ct);
        return new ClaveTemporal(gestor.Id, clave);
    }

    /// <summary>Nueva contraseña temporal; cierra sus sesiones y levanta el bloqueo. El segundo factor se conserva.</summary>
    public async Task<ClaveTemporal> RestablecerClave(SesionGestor oficial, int gestorId, CancellationToken ct = default)
    {
        var gestor = await Otro(oficial, gestorId, ct);
        var clave = Claves.GenerarClaveTemporal();

        gestor.HashClave = Claves.Hash(clave);
        gestor.DebeCambiarClave = true;
        gestor.IntentosFallidos = 0;
        gestor.BloqueadoHasta = null;
        gestor.SelloSesion = Guid.NewGuid();
        db.Registrar(TipoActor.Gestor, null, oficial.Id, AccionesBitacora.ClaveRestablecida, $"{gestor.Nombre} <{gestor.Correo}>");
        await db.SaveChangesAsync(ct);
        return new ClaveTemporal(gestor.Id, clave);
    }

    /// <summary>Borra el segundo factor (celular perdido): debe registrarlo de nuevo en su próximo ingreso.</summary>
    public async Task RestablecerTotp(SesionGestor oficial, int gestorId, CancellationToken ct = default)
    {
        var gestor = await Otro(oficial, gestorId, ct);

        gestor.TotpSecretoCifrado = null;
        gestor.TotpConfirmadoEn = null;
        gestor.TotpUltimoPaso = null;
        gestor.SelloSesion = Guid.NewGuid();
        db.Registrar(TipoActor.Gestor, null, oficial.Id, AccionesBitacora.TotpRestablecido, $"{gestor.Nombre} <{gestor.Correo}>");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Crea el primer oficial con <c>Semilla:Oficial:*</c> si la base no tiene gestores. Sin esa configuración no hace nada.
    /// Devuelve true si lo creó.
    /// </summary>
    public async Task<bool> SembrarOficial(IConfiguration configuracion, CancellationToken ct = default)
    {
        var seccion = configuracion.GetSection("Semilla:Oficial");
        var correo = seccion["Correo"];
        if (string.IsNullOrWhiteSpace(correo) || await db.Gestores.AnyAsync(ct))
        {
            return false;
        }

        var clave = seccion["Clave"] ?? "";
        if (clave.Length < AccesoGestorService.LongitudMinimaClave)
        {
            throw new InvalidOperationException(
                $"'Semilla:Oficial:Clave' debe tener al menos {AccesoGestorService.LongitudMinimaClave} caracteres.");
        }

        db.Gestores.Add(new Gestor
        {
            Nombre = Validar.Requerido(seccion["Nombre"], "nombre", Limites.Nombre),
            Correo = Validar.Correo(correo.Trim().ToLowerInvariant()),
            HashClave = Claves.Hash(clave),
            Rol = RolGestor.OficialCumplimiento,
            CreadoEn = reloj.Ahora(),
            SelloSesion = Guid.NewGuid(),
        });
        db.Registrar(TipoActor.Sistema, null, null, AccionesBitacora.OficialInicial);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>El oficial no se restablece a sí mismo: para eso usa el cambio de contraseña normal.</summary>
    private async Task<Gestor> Otro(SesionGestor oficial, int gestorId, CancellationToken ct)
    {
        if (gestorId == oficial.Id)
        {
            throw new ErrorValidacion("No puede restablecer su propia cuenta.");
        }

        return await db.Gestores.SingleOrDefaultAsync(g => g.Id == gestorId, ct)
               ?? throw new ErrorNoEncontrado("El gestor no existe.");
    }
}
