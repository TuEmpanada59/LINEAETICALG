using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LineaEtica.Domain;
using LineaEtica.Infrastructure.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Tests;

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class PublicoPruebas(BaseDatosPrueba baseDatos) : IDisposable
{
    private readonly ApiPrueba _api = new(baseDatos);

    public void Dispose() => _api.Dispose();

    private sealed record Acuse(string Codigo, string Clave, string TextoAcuse, DateTime VenceRespuestaEn);

    private sealed record Mensaje(string Autor, string Texto, DateTime Fecha);

    private sealed record Caso(string Codigo, string Estado, DateTime RecibidaEn, DateTime? VenceRespuestaEn, List<Mensaje> Mensajes);

    private sealed record Textos(int Aviso, int Autorizacion, int Empresa);

    private async Task<Textos> LeerFormulario(HttpClient cliente)
    {
        using var json = JsonDocument.Parse(await cliente.GetStringAsync("/api/publico/formulario"));
        var textos = json.RootElement.GetProperty("textos").EnumerateArray().ToList();
        int Id(string tipo) => textos.Single(t => t.GetProperty("tipo").GetString() == tipo).GetProperty("id").GetInt32();
        var empresa = json.RootElement.GetProperty("empresas")[0].GetProperty("id").GetInt32();
        return new Textos(Id("AvisoPrivacidad"), Id("Autorizacion"), empresa);
    }

    private static object Cuerpo(Textos t, string modo = "Anonimo", object? identidad = null, bool acepta = true,
        string relato = "Relato de prueba con información sensible", int? aviso = null) => new
    {
        empresaId = t.Empresa,
        modo,
        categoria = "Fraude",
        relato,
        fechaHechos = "marzo de 2026",
        lugar = "Bodega principal",
        personasInvolucradas = "Persona X",
        identidad,
        textoAvisoId = aviso ?? t.Aviso,
        textoAutorizacionId = t.Autorizacion,
        aceptaTratamiento = acepta,
    };

    private async Task<Acuse> Denunciar(HttpClient cliente, object cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/publico/denuncias", cuerpo);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<Acuse>())!;
    }

    private static Task<HttpResponseMessage> Ingresar(HttpClient cliente, string codigo, string clave) =>
        cliente.PostAsJsonAsync("/api/publico/seguimiento/ingresar", new { codigo, clave });

    [Fact]
    public async Task El_formulario_trae_empresas_y_los_tres_textos_vigentes()
    {
        using var json = JsonDocument.Parse(await _api.Cliente().GetStringAsync("/api/publico/formulario"));

        Assert.NotEmpty(json.RootElement.GetProperty("empresas").EnumerateArray());
        Assert.Equal(3, json.RootElement.GetProperty("textos").GetArrayLength());
    }

    [Fact]
    public async Task Una_denuncia_anonima_da_acuse_y_no_guarda_datos_personales_ni_texto_legible()
    {
        var cliente = _api.Cliente();
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente)));

        Assert.Matches("^LG-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$", acuse.Codigo);
        Assert.Matches("^([A-HJ-NP-Z2-9]{4}-){3}[A-HJ-NP-Z2-9]{4}$", acuse.Clave);
        Assert.False(string.IsNullOrWhiteSpace(acuse.TextoAcuse));

        await using var db = baseDatos.CrearContexto();
        var denuncia = await db.Denuncias.Include(d => d.Identidad).SingleAsync(d => d.Codigo == acuse.Codigo);
        Assert.Null(denuncia.Identidad);
        Assert.Equal(ModoDenuncia.Anonimo, denuncia.Modo);
        Assert.DoesNotContain("información sensible", Encoding.UTF8.GetString(denuncia.RelatoCifrado!));
        Assert.DoesNotContain(acuse.Clave, denuncia.HashClave);
        Assert.True(Claves.Verificar(acuse.Clave, denuncia.HashClave));
        Assert.Equal(denuncia.CreadaEn.AddDays(15), denuncia.VenceRespuestaEn);
        Assert.True(await db.Actuaciones.AnyAsync(a => a.DenunciaId == denuncia.Id && a.Accion == AccionesBitacora.DenunciaRecibida));
    }

    [Fact]
    public async Task Una_denuncia_a_nombre_propio_guarda_la_identidad_cifrada()
    {
        var cliente = _api.Cliente();
        var identidad = new { nombre = "Ana Pérez", correo = "ana.perez@correo.co", telefono = "+57 300 000 0000", relacion = "Proveedor" };
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente), "ANombrePropio", identidad));

        await using var db = baseDatos.CrearContexto();
        var denuncia = await db.Denuncias.Include(d => d.Identidad).SingleAsync(d => d.Codigo == acuse.Codigo);
        Assert.NotNull(denuncia.Identidad);
        Assert.Equal(RelacionDenunciante.Proveedor, denuncia.Identidad.Relacion);
        Assert.DoesNotContain("ana.perez", Encoding.UTF8.GetString(denuncia.Identidad.CorreoCifrado));

        var cifrado = new CifradoDatos(new OpcionesCifrado { Llave = ApiPrueba.Llave });
        Assert.Equal("Ana Pérez", cifrado.Descifrar(denuncia.Identidad.NombreCifrado, ContextoCifrado.Nombre(denuncia.Id)));
    }

    public static TheoryData<string, string> CasosInvalidos => new()
    {
        { "anonima-con-identidad", "Una denuncia anónima no lleva datos personales." },
        { "propia-sin-identidad", "Una denuncia a nombre propio requiere nombre, correo y relación." },
        { "sin-autorizacion", "Debe autorizar el tratamiento de datos para enviar la denuncia." },
        { "sin-relato", "El campo «relato» es obligatorio." },
        { "correo-invalido", "El correo no es válido." },
        { "texto-legal-viejo", "Los textos legales se actualizaron. Recargue la página y vuelva a aceptarlos." },
    };

    [Theory]
    [MemberData(nameof(CasosInvalidos))]
    public async Task Rechaza_denuncias_invalidas_con_un_mensaje_claro(string caso, string mensaje)
    {
        var cliente = _api.Cliente();
        var t = await LeerFormulario(cliente);
        var identidad = new { nombre = "Ana", correo = "ana@correo.co", relacion = "Empleado" };
        var cuerpo = caso switch
        {
            "anonima-con-identidad" => Cuerpo(t, identidad: identidad),
            "propia-sin-identidad" => Cuerpo(t, "ANombrePropio"),
            "sin-autorizacion" => Cuerpo(t, acepta: false),
            "sin-relato" => Cuerpo(t, relato: "   "),
            "correo-invalido" => Cuerpo(t, "ANombrePropio", new { nombre = "Ana", correo = "no-es-correo", relacion = "Empleado" }),
            _ => Cuerpo(t, aviso: t.Aviso + 1000),
        };

        var respuesta = await cliente.PostAsJsonAsync("/api/publico/denuncias", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Un_cuerpo_mal_formado_responde_400_en_espanol_sin_detalles_tecnicos()
    {
        var respuesta = await _api.Cliente().PostAsync("/api/publico/denuncias",
            new StringContent("{\"modo\": \"NoExiste\"}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("La solicitud no tiene el formato esperado.", texto);
        Assert.DoesNotContain("JSON", texto);
    }

    [Fact]
    public async Task El_seguimiento_permite_ver_el_estado_y_usar_el_buzon()
    {
        var cliente = _api.Cliente();
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/publico/seguimiento")).StatusCode);

        // Acepta el código y la contraseña escritos en minúsculas.
        var ingreso = await Ingresar(cliente, acuse.Codigo.ToLowerInvariant(), acuse.Clave.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.NoContent, ingreso.StatusCode);
        var cookie = Assert.Single(ingreso.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var caso = await cliente.GetFromJsonAsync<Caso>("/api/publico/seguimiento");
        Assert.Equal(acuse.Codigo, caso!.Codigo);
        Assert.Equal("Recibida", caso.Estado);
        Assert.Empty(caso.Mensajes);

        var envio = await cliente.PostAsJsonAsync("/api/publico/seguimiento/mensajes", new { texto = "Información nueva" });
        Assert.Equal(HttpStatusCode.NoContent, envio.StatusCode);

        caso = await cliente.GetFromJsonAsync<Caso>("/api/publico/seguimiento");
        var mensaje = Assert.Single(caso!.Mensajes);
        Assert.Equal("Denunciante", mensaje.Autor);
        Assert.Equal("Información nueva", mensaje.Texto);

        await using var db = baseDatos.CrearContexto();
        var id = await db.Denuncias.Where(d => d.Codigo == acuse.Codigo).Select(d => d.Id).SingleAsync();
        Assert.Equal(2, await db.Actuaciones.CountAsync(a => a.DenunciaId == id && a.Accion == AccionesBitacora.ConsultaDenunciante));
        Assert.Equal(1, await db.Actuaciones.CountAsync(a => a.DenunciaId == id && a.Accion == AccionesBitacora.MensajeDenunciante));

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync("/api/publico/seguimiento/salir", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/publico/seguimiento")).StatusCode);
    }

    [Fact]
    public async Task Codigo_inexistente_y_clave_incorrecta_responden_igual()
    {
        var cliente = _api.Cliente();
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente)));

        var inexistente = await Ingresar(cliente, "LG-ZZZZ-ZZZZ", acuse.Clave);
        var incorrecta = await Ingresar(cliente, acuse.Codigo, "AAAA-AAAA-AAAA-AAAA");

        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        Assert.Equal(await Mensaje401(inexistente), await Mensaje401(incorrecta));
    }

    private static async Task<string> Mensaje401(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return $"{json.RootElement.GetProperty("title")}|{json.RootElement.GetProperty("detail")}";
    }

    [Fact]
    public async Task Tras_cinco_intentos_fallidos_se_bloquea_aunque_luego_use_la_clave_correcta()
    {
        var cliente = _api.Cliente();
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente)));

        for (var i = 0; i < SeguimientoService.IntentosPermitidos; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Ingresar(cliente, acuse.Codigo, "AAAA-AAAA-AAAA-AAAA")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Ingresar(cliente, acuse.Codigo, acuse.Clave)).StatusCode);

        await using var db = baseDatos.CrearContexto();
        var denuncia = await db.Denuncias.SingleAsync(d => d.Codigo == acuse.Codigo);
        Assert.True(denuncia.BloqueadaHasta > DateTime.UtcNow);
        Assert.True(await db.Actuaciones.AnyAsync(a => a.DenunciaId == denuncia.Id && a.Accion == AccionesBitacora.SeguimientoBloqueado));
    }

    [Fact]
    public async Task Un_caso_purgado_ya_no_permite_ingresar()
    {
        var cliente = _api.Cliente();
        var acuse = await Denunciar(cliente, Cuerpo(await LeerFormulario(cliente)));

        await using (var db = baseDatos.CrearContexto())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE dbo.Denuncias
                SET Estado = 4, CerradaEn = SYSUTCDATETIME(), BorrarDatosEn = SYSUTCDATETIME(), MotivoCierre = 1,
                    RelatoCifrado = NULL, HashClave = NULL, FechaHechosCifrada = NULL, LugarCifrado = NULL,
                    PersonasInvolucradasCifrado = NULL, DatosPersonalesEliminados = 1
                WHERE Codigo = {acuse.Codigo}
                """);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Ingresar(cliente, acuse.Codigo, acuse.Clave)).StatusCode);
    }
}
