using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Infrastructure.Servicios;

public sealed class DenunciaService(
    LineaEticaDbContext db,
    CifradoDatos cifrado,
    ParametrosCanal parametros,
    TimeProvider reloj)
{
    public async Task<Formulario> ObtenerFormulario(CancellationToken ct = default)
    {
        var empresas = await db.Empresas.AsNoTracking()
            .Where(e => e.Activa)
            .OrderBy(e => e.Nombre)
            .Select(e => new EmpresaOpcion(e.Id, e.Nombre))
            .ToListAsync(ct);

        return new Formulario(empresas, await TextosVigentes(ct));
    }

    public async Task<DenunciaRecibida> Crear(NuevaDenuncia datos, CancellationToken ct = default)
    {
        if (!datos.AceptaTratamiento)
        {
            throw new ErrorValidacion("Debe autorizar el tratamiento de datos para enviar la denuncia.");
        }

        var modo = Validar.Opcion(datos.Modo, "tipo de denuncia");
        var categoria = Validar.Opcion(datos.Categoria, "conducta");
        var relato = Validar.Requerido(datos.Relato, "relato", Limites.Relato);
        var fechaHechos = Validar.Opcional(datos.FechaHechos, "fecha de los hechos", Limites.FechaHechos);
        var lugar = Validar.Opcional(datos.Lugar, "lugar", Limites.Lugar);
        var personas = Validar.Opcional(datos.PersonasInvolucradas, "personas involucradas", Limites.PersonasInvolucradas);

        var identidad = (modo, datos.Identidad) switch
        {
            (ModoDenuncia.Anonimo, null) => null,
            (ModoDenuncia.Anonimo, _) => throw new ErrorValidacion("Una denuncia anónima no lleva datos personales."),
            (_, null) => throw new ErrorValidacion("Una denuncia a nombre propio requiere nombre, correo y relación."),
            (_, var d) => new
            {
                Nombre = Validar.Requerido(d.Nombre, "nombre", Limites.Nombre),
                Correo = Validar.Correo(d.Correo),
                Telefono = Validar.Telefono(d.Telefono),
                Relacion = Validar.Opcion(d.Relacion, "relación con la empresa"),
            },
        };

        if (!await db.Empresas.AnyAsync(e => e.Id == datos.EmpresaId && e.Activa, ct))
        {
            throw new ErrorValidacion("Seleccione una empresa válida.");
        }

        var vigentes = await TextosVigentes(ct);
        if (!vigentes.Any(t => t.Tipo == TipoTextoLegal.AvisoPrivacidad && t.Id == datos.TextoAvisoId) ||
            !vigentes.Any(t => t.Tipo == TipoTextoLegal.Autorizacion && t.Id == datos.TextoAutorizacionId))
        {
            throw new ErrorValidacion("Los textos legales se actualizaron. Recargue la página y vuelva a aceptarlos.");
        }

        var ahora = reloj.Ahora();
        var vence = ahora.AddDays(await parametros.PlazoRespuestaDias(ct));
        var id = Guid.CreateVersion7();
        var clave = Claves.GenerarClaveSeguimiento();

        string codigo;
        do
        {
            codigo = Claves.GenerarCodigo();
        }
        while (await db.Denuncias.AnyAsync(d => d.Codigo == codigo, ct));

        var denuncia = new Denuncia
        {
            Id = id,
            Codigo = codigo,
            HashClave = Claves.Hash(clave),
            EmpresaId = datos.EmpresaId,
            Modo = modo,
            Categoria = categoria,
            RelatoCifrado = cifrado.Cifrar(relato, ContextoCifrado.Relato(id)),
            FechaHechosCifrada = fechaHechos is null ? null : cifrado.Cifrar(fechaHechos, ContextoCifrado.FechaHechos(id)),
            LugarCifrado = lugar is null ? null : cifrado.Cifrar(lugar, ContextoCifrado.Lugar(id)),
            PersonasInvolucradasCifrado = personas is null ? null : cifrado.Cifrar(personas, ContextoCifrado.PersonasInvolucradas(id)),
            CreadaEn = ahora,
            VenceRespuestaEn = vence,
            AceptoTratamientoEn = ahora,
            TextoAvisoId = datos.TextoAvisoId,
            TextoAutorizacionId = datos.TextoAutorizacionId,
        };

        if (identidad is not null)
        {
            denuncia.Identidad = new Identidad
            {
                NombreCifrado = cifrado.Cifrar(identidad.Nombre, ContextoCifrado.Nombre(id)),
                CorreoCifrado = cifrado.Cifrar(identidad.Correo, ContextoCifrado.Correo(id)),
                TelefonoCifrado = identidad.Telefono is null ? null : cifrado.Cifrar(identidad.Telefono, ContextoCifrado.Telefono(id)),
                Relacion = identidad.Relacion,
            };
        }

        db.Denuncias.Add(denuncia);
        db.Registrar(TipoActor.Denunciante, id, null, AccionesBitacora.DenunciaRecibida);
        await db.SaveChangesAsync(ct);

        return new DenunciaRecibida(codigo, clave, await parametros.TextoAcuse(ct), vence);
    }

    private async Task<IReadOnlyList<TextoVigente>> TextosVigentes(CancellationToken ct)
    {
        var textos = await db.TextosLegales.AsNoTracking().ToListAsync(ct);
        return textos
            .GroupBy(t => t.Tipo)
            .Select(g => g.MaxBy(t => t.Version)!)
            .OrderBy(t => t.Tipo)
            .Select(t => new TextoVigente(t.Id, t.Tipo, t.Version, t.Contenido))
            .ToList();
    }
}
