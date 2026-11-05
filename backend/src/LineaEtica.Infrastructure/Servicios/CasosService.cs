using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

/// <summary>
/// Gestión de casos en el panel. El oficial ve todos; un gestor, solo los que tiene asignados.
/// Un caso sin acceso responde igual que uno inexistente.
/// </summary>
public sealed class CasosService(LineaEticaDbContext db, CifradoDatos cifrado, ParametrosCanal parametros, TimeProvider reloj)
{
    private const string NoEncontrado = "El caso no existe o no tiene acceso a él.";

    public async Task<IReadOnlyList<CasoResumen>> Listar(SesionGestor sesion, EstadoDenuncia? estado, CancellationToken ct = default)
    {
        var consulta = Visibles(sesion);
        if (estado is not null)
        {
            consulta = consulta.Where(d => d.Estado == estado);
        }

        return await consulta
            .OrderByDescending(d => d.CreadaEn)
            .Select(d => new CasoResumen(
                d.Id,
                d.Codigo,
                d.Empresa!.Nombre,
                d.Categoria,
                d.Modo,
                d.Estado,
                d.CreadaEn,
                d.VenceRespuestaEn,
                d.Asignaciones.OrderBy(a => a.AsignadaEn).Select(a => a.Gestor!.Nombre).ToList()))
            .ToListAsync(ct);
    }

    /// <summary>Detalle con el contenido descifrado. Cada consulta queda en la bitácora.</summary>
    public async Task<CasoGestion> Obtener(SesionGestor sesion, Guid id, CancellationToken ct = default)
    {
        var d = await Visibles(sesion).AsNoTracking()
                    .Include(x => x.Empresa)
                    .Include(x => x.Identidad)
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new ErrorNoEncontrado(NoEncontrado);

        var asignados = await db.Asignaciones.AsNoTracking()
            .Where(a => a.DenunciaId == id)
            .OrderBy(a => a.AsignadaEn)
            .Select(a => new AsignadoCaso(a.GestorId, a.Gestor!.Nombre, a.AsignadaEn))
            .ToListAsync(ct);

        var mensajes = await db.Mensajes.AsNoTracking()
            .Where(m => m.DenunciaId == id)
            .OrderBy(m => m.CreadoEn).ThenBy(m => m.Id)
            .Select(m => new { m.Autor, Gestor = m.Gestor!.Nombre, m.TextoCifrado, m.CreadoEn })
            .ToListAsync(ct);

        var anotaciones = await db.Anotaciones.AsNoTracking()
            .Where(a => a.DenunciaId == id)
            .OrderBy(a => a.CreadaEn).ThenBy(a => a.Id)
            .Select(a => new { Gestor = a.Gestor!.Nombre, a.TextoCifrado, a.CreadaEn })
            .ToListAsync(ct);

        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.ConsultaCaso);
        await db.SaveChangesAsync(ct);

        // La bitácora se lee después de registrar la consulta, para que la incluya.
        var bitacora = await db.Actuaciones.AsNoTracking()
            .Where(a => a.DenunciaId == id)
            .OrderByDescending(a => a.CreadaEn).ThenByDescending(a => a.Id)
            .Select(a => new ActuacionCaso(a.ActorTipo, a.Gestor!.Nombre, a.Accion, a.Detalle, a.CreadaEn))
            .ToListAsync(ct);

        string? Leer(byte[]? datos, string contexto) => datos is null ? null : cifrado.Descifrar(datos, contexto);

        var identidad = d.Identidad is null
            ? null
            : new IdentidadCaso(
                cifrado.Descifrar(d.Identidad.NombreCifrado, ContextoCifrado.Nombre(id)),
                cifrado.Descifrar(d.Identidad.CorreoCifrado, ContextoCifrado.Correo(id)),
                Leer(d.Identidad.TelefonoCifrado, ContextoCifrado.Telefono(id)),
                d.Identidad.Relacion);

        return new CasoGestion(
            d.Id,
            d.Codigo,
            d.Empresa!.Nombre,
            d.Modo,
            d.Categoria,
            d.Estado,
            d.MotivoCierre,
            d.CreadaEn,
            d.VenceRespuestaEn,
            d.CerradaEn,
            d.BorrarDatosEn,
            d.DatosPersonalesEliminados,
            Leer(d.RelatoCifrado, ContextoCifrado.Relato(id)),
            Leer(d.FechaHechosCifrada, ContextoCifrado.FechaHechos(id)),
            Leer(d.LugarCifrado, ContextoCifrado.Lugar(id)),
            Leer(d.PersonasInvolucradasCifrado, ContextoCifrado.PersonasInvolucradas(id)),
            identidad,
            asignados,
            mensajes.Select(m => new MensajeGestion(
                m.Autor, m.Gestor, cifrado.Descifrar(m.TextoCifrado, ContextoCifrado.Mensaje(id)), m.CreadoEn)).ToList(),
            anotaciones.Select(a => new AnotacionCaso(
                a.Gestor, cifrado.Descifrar(a.TextoCifrado, ContextoCifrado.Anotacion(id)), a.CreadaEn)).ToList(),
            bitacora);
    }

    /// <summary>
    /// Cambia el estado. Cerrar exige motivo y programa la purga de los datos; un caso cerrado no se reabre.
    /// </summary>
    public async Task CambiarEstado(SesionGestor sesion, Guid id, CambioEstado cambio, CancellationToken ct = default)
    {
        var nuevo = Validar.Opcion(cambio.Estado, "estado");
        var denuncia = await Editable(sesion, id, ct);

        if (denuncia.Estado == EstadoDenuncia.Cerrada)
        {
            throw new ErrorValidacion("El caso está cerrado y no puede cambiar de estado.");
        }

        if (denuncia.Estado == nuevo)
        {
            throw new ErrorValidacion("El caso ya está en ese estado.");
        }

        var anterior = denuncia.Estado;
        denuncia.Estado = nuevo;

        if (nuevo == EstadoDenuncia.Cerrada)
        {
            var motivo = cambio.MotivoCierre is { } m
                ? Validar.Opcion(m, "motivo de cierre")
                : throw new ErrorValidacion("Para cerrar el caso indique el motivo de cierre.");
            var ahora = reloj.Ahora();
            denuncia.MotivoCierre = motivo;
            denuncia.CerradaEn = ahora;
            denuncia.BorrarDatosEn = ahora.AddDays(await parametros.RetencionDiasTrasCierre(ct));
        }
        else if (cambio.MotivoCierre is not null)
        {
            throw new ErrorValidacion("El motivo de cierre solo aplica al cerrar el caso.");
        }

        var detalle = nuevo == EstadoDenuncia.Cerrada ? $"{anterior} → {nuevo} ({denuncia.MotivoCierre})" : $"{anterior} → {nuevo}";
        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.CambioEstado, detalle);
        await Guardar(ct);
    }

    /// <summary>Solo el oficial asigna. Nunca es automático, aunque la denuncia involucre a un gestor.</summary>
    public async Task Asignar(SesionGestor sesion, Guid id, int gestorId, CancellationToken ct = default)
    {
        await Editable(sesion, id, ct);

        var gestor = await db.Gestores.AsNoTracking()
                         .Where(g => g.Id == gestorId && g.Activo)
                         .Select(g => new { g.Nombre })
                         .SingleOrDefaultAsync(ct)
                     ?? throw new ErrorValidacion("El gestor no existe o está inactivo.");

        if (await db.Asignaciones.AnyAsync(a => a.DenunciaId == id && a.GestorId == gestorId, ct))
        {
            throw new ErrorValidacion("El caso ya está asignado a ese gestor.");
        }

        db.Asignaciones.Add(new Asignacion { DenunciaId = id, GestorId = gestorId, AsignadaPorId = sesion.Id, AsignadaEn = reloj.Ahora() });
        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.Asignacion, $"A {gestor.Nombre}");
        await db.SaveChangesAsync(ct);
    }

    public async Task Desasignar(SesionGestor sesion, Guid id, int gestorId, CancellationToken ct = default)
    {
        await Editable(sesion, id, ct);

        var asignacion = await db.Asignaciones.Include(a => a.Gestor)
                             .SingleOrDefaultAsync(a => a.DenunciaId == id && a.GestorId == gestorId, ct)
                         ?? throw new ErrorValidacion("El caso no está asignado a ese gestor.");

        db.Asignaciones.Remove(asignacion);
        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.Desasignacion, $"A {asignacion.Gestor!.Nombre}");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Respuesta al denunciante en el buzón. Se permite también con el caso cerrado.</summary>
    public async Task Responder(SesionGestor sesion, Guid id, string? texto, CancellationToken ct = default)
    {
        var contenido = Validar.Requerido(texto, "mensaje", Limites.Mensaje);
        await Editable(sesion, id, ct);

        db.Mensajes.Add(new Mensaje
        {
            DenunciaId = id,
            Autor = AutorMensaje.Empresa,
            GestorId = sesion.Id,
            TextoCifrado = cifrado.Cifrar(contenido, ContextoCifrado.Mensaje(id)),
            CreadoEn = reloj.Ahora(),
        });
        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.MensajeEmpresa);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Nota interna: el denunciante nunca la ve.</summary>
    public async Task Anotar(SesionGestor sesion, Guid id, string? texto, CancellationToken ct = default)
    {
        var contenido = Validar.Requerido(texto, "anotación", Limites.Mensaje);
        await Editable(sesion, id, ct);

        db.Anotaciones.Add(new Anotacion
        {
            DenunciaId = id,
            GestorId = sesion.Id,
            TextoCifrado = cifrado.Cifrar(contenido, ContextoCifrado.Anotacion(id)),
            CreadaEn = reloj.Ahora(),
        });
        db.Registrar(TipoActor.Gestor, id, sesion.Id, AccionesBitacora.Anotacion);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Denuncia> Visibles(SesionGestor sesion) =>
        sesion.Rol == RolGestor.OficialCumplimiento
            ? db.Denuncias
            : db.Denuncias.Where(d => d.Asignaciones.Any(a => a.GestorId == sesion.Id));

    /// <summary>Caso visible y con datos vigentes: después de la purga solo queda la consulta.</summary>
    private async Task<Denuncia> Editable(SesionGestor sesion, Guid id, CancellationToken ct)
    {
        var denuncia = await Visibles(sesion).SingleOrDefaultAsync(d => d.Id == id, ct)
                       ?? throw new ErrorNoEncontrado(NoEncontrado);

        return denuncia.DatosPersonalesEliminados
            ? throw new ErrorValidacion("Los datos de este caso ya fueron eliminados; no admite cambios.")
            : denuncia;
    }

    private async Task Guardar(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ErrorConflicto("Otra persona modificó el caso al mismo tiempo. Recárguelo e intente de nuevo.");
        }
    }
}
