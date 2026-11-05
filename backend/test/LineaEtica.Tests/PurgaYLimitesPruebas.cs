using System.Net;
using System.Net.Http.Json;
using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Tests;

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class PurgaYLimitesPruebas(BaseDatosPrueba baseDatos)
{
    private readonly CifradoDatos _cifrado = new(new OpcionesCifrado { Llave = ApiPrueba.Llave });

    /// <summary>Caso completo (identidad, mensaje, anotación) creado directo en la base.</summary>
    private async Task<Guid> CrearCaso(EstadoDenuncia estado, DateTime? borrarDatosEn)
    {
        await using var db = baseDatos.CrearContexto();
        var gestor = new Gestor
        {
            Nombre = "Gestor purga",
            Correo = $"p{Guid.NewGuid():N}@prueba.co",
            HashClave = Claves.Hash("Clave-de-prueba-123"),
            Rol = RolGestor.OficialCumplimiento,
            SelloSesion = Guid.NewGuid(),
        };
        db.Gestores.Add(gestor);

        var id = Guid.CreateVersion7();
        var cerrada = estado == EstadoDenuncia.Cerrada;
        var denuncia = new Denuncia
        {
            Id = id,
            Codigo = Claves.GenerarCodigo(),
            HashClave = Claves.Hash("clave"),
            EmpresaId = await db.Empresas.Select(e => e.Id).FirstAsync(),
            Modo = ModoDenuncia.ANombrePropio,
            Categoria = CategoriaConducta.Fraude,
            RelatoCifrado = _cifrado.Cifrar("relato", ContextoCifrado.Relato(id)),
            LugarCifrado = _cifrado.Cifrar("lugar", ContextoCifrado.Lugar(id)),
            Estado = estado,
            CreadaEn = DateTime.UtcNow.AddYears(-2),
            CerradaEn = cerrada ? DateTime.UtcNow.AddYears(-1) : null,
            BorrarDatosEn = cerrada ? borrarDatosEn : null,
            MotivoCierre = cerrada ? MotivoCierre.Fundada : null,
            AceptoTratamientoEn = DateTime.UtcNow.AddYears(-2),
            TextoAvisoId = await db.TextosLegales.Where(t => t.Tipo == TipoTextoLegal.AvisoPrivacidad).Select(t => t.Id).FirstAsync(),
            TextoAutorizacionId = await db.TextosLegales.Where(t => t.Tipo == TipoTextoLegal.Autorizacion).Select(t => t.Id).FirstAsync(),
            Identidad = new Identidad
            {
                NombreCifrado = _cifrado.Cifrar("Nombre", ContextoCifrado.Nombre(id)),
                CorreoCifrado = _cifrado.Cifrar("c@prueba.co", ContextoCifrado.Correo(id)),
                Relacion = RelacionDenunciante.Empleado,
            },
        };
        db.Denuncias.Add(denuncia);
        await db.SaveChangesAsync();

        db.Mensajes.Add(new Mensaje { DenunciaId = id, Autor = AutorMensaje.Denunciante, TextoCifrado = _cifrado.Cifrar("m", ContextoCifrado.Mensaje(id)) });
        db.Anotaciones.Add(new Anotacion { DenunciaId = id, GestorId = gestor.Id, TextoCifrado = _cifrado.Cifrar("a", ContextoCifrado.Anotacion(id)) });
        db.Asignaciones.Add(new Asignacion { DenunciaId = id, GestorId = gestor.Id, AsignadaPorId = gestor.Id });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task La_purga_borra_el_contenido_de_los_casos_vencidos_y_conserva_lo_demas()
    {
        var vencido = await CrearCaso(EstadoDenuncia.Cerrada, DateTime.UtcNow.AddDays(-1));
        var vigente = await CrearCaso(EstadoDenuncia.Cerrada, DateTime.UtcNow.AddDays(30));
        var abierto = await CrearCaso(EstadoDenuncia.EnRevision, null);

        await using (var db = baseDatos.CrearContexto())
        {
            Assert.True(await new PurgaDatosService(db, TimeProvider.System).Purgar() >= 1);
            Assert.Equal(0, await new PurgaDatosService(db, TimeProvider.System).Purgar());
        }

        await using var verificacion = baseDatos.CrearContexto();
        var purgado = await verificacion.Denuncias.SingleAsync(d => d.Id == vencido);
        Assert.True(purgado.DatosPersonalesEliminados);
        Assert.Null(purgado.RelatoCifrado);
        Assert.Null(purgado.LugarCifrado);
        Assert.Null(purgado.HashClave);
        Assert.False(await verificacion.Identidades.AnyAsync(i => i.DenunciaId == vencido));
        Assert.False(await verificacion.Mensajes.AnyAsync(m => m.DenunciaId == vencido));
        Assert.False(await verificacion.Anotaciones.AnyAsync(a => a.DenunciaId == vencido));
        Assert.True(await verificacion.Asignaciones.AnyAsync(a => a.DenunciaId == vencido));
        Assert.True(await verificacion.Actuaciones.AnyAsync(a =>
            a.DenunciaId == vencido && a.ActorTipo == TipoActor.Sistema && a.Accion == AccionesBitacora.DatosEliminados));

        foreach (var id in new[] { vigente, abierto })
        {
            var intacto = await verificacion.Denuncias.SingleAsync(d => d.Id == id);
            Assert.False(intacto.DatosPersonalesEliminados);
            Assert.True(await verificacion.Identidades.AnyAsync(i => i.DenunciaId == id));
            Assert.True(await verificacion.Mensajes.AnyAsync(m => m.DenunciaId == id));
        }
    }

    [Fact]
    public async Task La_purga_funciona_con_los_permisos_del_rol_de_la_API()
    {
        var vencido = await CrearCaso(EstadoDenuncia.Cerrada, DateTime.UtcNow.AddDays(-1));

        await using var db = baseDatos.CrearContexto();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("EXECUTE AS USER = 'usuario_api_prueba';");
        try
        {
            await new PurgaDatosService(db, TimeProvider.System).Purgar();
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("REVERT;");
        }

        Assert.True(await db.Denuncias.AnyAsync(d => d.Id == vencido && d.DatosPersonalesEliminados));
    }

    [Fact]
    public async Task Pasado_el_limite_por_IP_el_ingreso_responde_429_en_espanol()
    {
        using var api = new ApiPrueba(baseDatos, new Dictionary<string, string> { ["LimitesIp:Ingresos"] = "3" });
        var cliente = api.Cliente();

        for (var i = 0; i < 3; i++)
        {
            var permitido = await cliente.PostAsJsonAsync("/api/gestion/ingresar", new { correo = "x@prueba.co", clave = "x" });
            Assert.Equal(HttpStatusCode.Unauthorized, permitido.StatusCode);
        }

        // El contador es por IP y compartido: también frena el ingreso del seguimiento.
        var rechazado = await cliente.PostAsJsonAsync("/api/publico/seguimiento/ingresar", new { codigo = "LG-AAAA-AAAA", clave = "x" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rechazado.StatusCode);
        Assert.Contains("Demasiados intentos", await rechazado.Content.ReadAsStringAsync());
        Assert.True(rechazado.Headers.Contains("Retry-After"));
    }
}
