using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using LineaEtica.Infrastructure.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LineaEtica.Tests;

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class GestionPruebas(BaseDatosPrueba baseDatos) : IDisposable
{
    private const string ClaveDefinitiva = "Clave-definitiva-2026";

    private readonly ApiPrueba _api = new(baseDatos);
    private readonly CifradoDatos _cifrado = new(new OpcionesCifrado { Llave = ApiPrueba.Llave });

    public void Dispose() => _api.Dispose();

    private sealed record Cuenta(int Id, string Correo, string Clave, byte[] Secreto);

    private sealed record Sesion(string Nombre, string Rol, bool DebeCambiarClave, bool TotpConfirmado, bool Completa);

    private sealed record Temporal(int GestorId, string Clave);

    private static long PasoActual() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

    /// <summary>Gestor listo para usar (clave definitiva y TOTP registrado), creado directo en la base.</summary>
    private async Task<Cuenta> CrearCuenta(RolGestor rol, string nombre = "Gestor de prueba")
    {
        var correo = $"g{Guid.NewGuid():N}@prueba.co";
        var secreto = Totp.GenerarSecreto();
        await using var db = baseDatos.CrearContexto();
        var gestor = new Gestor
        {
            Nombre = nombre,
            Correo = correo,
            HashClave = Claves.Hash(ClaveDefinitiva),
            Rol = rol,
            DebeCambiarClave = false,
            SelloSesion = Guid.NewGuid(),
            TotpConfirmadoEn = DateTime.UtcNow,
        };
        db.Gestores.Add(gestor);
        await db.SaveChangesAsync();
        gestor.TotpSecretoCifrado = _cifrado.Cifrar(Convert.ToBase64String(secreto), ContextoCifrado.SecretoTotp(gestor.Id));
        await db.SaveChangesAsync();
        return new Cuenta(gestor.Id, correo, ClaveDefinitiva, secreto);
    }

    private static Task<HttpResponseMessage> Ingresar(HttpClient cliente, string correo, string clave, string? codigo) =>
        cliente.PostAsJsonAsync("/api/gestion/ingresar", new { correo, clave, codigo });

    /// <summary>Ingresa con el código del paso indicado (por defecto el actual).</summary>
    private async Task<HttpClient> Entrar(Cuenta cuenta, long? paso = null)
    {
        var cliente = _api.Cliente();
        var respuesta = await Ingresar(cliente, cuenta.Correo, cuenta.Clave, Totp.Calcular(cuenta.Secreto, paso ?? PasoActual()));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return cliente;
    }

    private async Task<(Guid Id, string Codigo, string Clave)> CrearDenuncia(object? identidad = null)
    {
        var cliente = _api.Cliente();
        using var formulario = JsonDocument.Parse(await cliente.GetStringAsync("/api/publico/formulario"));
        var textos = formulario.RootElement.GetProperty("textos").EnumerateArray().ToList();
        int Texto(string tipo) => textos.Single(t => t.GetProperty("tipo").GetString() == tipo).GetProperty("id").GetInt32();

        var respuesta = await cliente.PostAsJsonAsync("/api/publico/denuncias", new
        {
            empresaId = formulario.RootElement.GetProperty("empresas")[0].GetProperty("id").GetInt32(),
            modo = identidad is null ? "Anonimo" : "ANombrePropio",
            categoria = "Soborno",
            relato = "Relato confidencial del caso",
            lugar = "Oficina norte",
            identidad,
            textoAvisoId = Texto("AvisoPrivacidad"),
            textoAutorizacionId = Texto("Autorizacion"),
            aceptaTratamiento = true,
        });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var acuse = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var codigo = acuse.RootElement.GetProperty("codigo").GetString()!;

        await using var db = baseDatos.CrearContexto();
        var id = await db.Denuncias.Where(d => d.Codigo == codigo).Select(d => d.Id).SingleAsync();
        return (id, codigo, acuse.RootElement.GetProperty("clave").GetString()!);
    }

    private static async Task<List<Guid>> IdsDeCasos(HttpClient cliente)
    {
        using var json = JsonDocument.Parse(await cliente.GetStringAsync("/api/gestion/casos"));
        return json.RootElement.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
    }

    [Fact]
    public async Task Un_gestor_nuevo_cambia_la_clave_y_registra_el_TOTP_antes_de_ver_casos()
    {
        var oficial = await Entrar(await CrearCuenta(RolGestor.OficialCumplimiento));
        var correo = $"nuevo{Guid.NewGuid():N}@prueba.co";
        var creada = await oficial.PostAsJsonAsync("/api/gestion/gestores", new { nombre = "Gestor Nuevo", correo, rol = "Gestor" });
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        var temporal = (await creada.Content.ReadFromJsonAsync<Temporal>())!;

        // Primer ingreso: sin TOTP registrado, la clave temporal abre una sesión incompleta.
        var cliente = _api.Cliente();
        var ingreso = await Ingresar(cliente, correo, temporal.Clave, null);
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        var sesion = (await ingreso.Content.ReadFromJsonAsync<Sesion>())!;
        Assert.True(sesion.DebeCambiarClave);
        Assert.False(sesion.Completa);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/gestion/casos")).StatusCode);

        var corta = await cliente.PostAsJsonAsync("/api/gestion/clave", new { claveActual = temporal.Clave, claveNueva = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);
        var cambio = await cliente.PostAsJsonAsync("/api/gestion/clave", new { claveActual = temporal.Clave, claveNueva = ClaveDefinitiva });
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);

        using var registro = JsonDocument.Parse(await (await cliente.PostAsync("/api/gestion/totp/registro", null)).Content.ReadAsStringAsync());
        Assert.StartsWith("otpauth://totp/", registro.RootElement.GetProperty("uri").GetString());

        await using (var db = baseDatos.CrearContexto())
        {
            var guardado = await db.Gestores.SingleAsync(g => g.Id == temporal.GestorId);
            var secreto = Convert.FromBase64String(_cifrado.Descifrar(guardado.TotpSecretoCifrado!, ContextoCifrado.SecretoTotp(guardado.Id)));
            Assert.Equal(registro.RootElement.GetProperty("secreto").GetString(), Totp.ABase32(secreto));

            Assert.Equal(HttpStatusCode.BadRequest,
                (await cliente.PostAsJsonAsync("/api/gestion/totp/confirmar", new { codigo = "000000" })).StatusCode);
            var confirmacion = await cliente.PostAsJsonAsync("/api/gestion/totp/confirmar",
                new { codigo = Totp.Calcular(secreto, PasoActual()) });
            Assert.True((await confirmacion.Content.ReadFromJsonAsync<Sesion>())!.Completa);
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/gestion/casos")).StatusCode);

            // Después ya no basta la contraseña: exige el código, y cada código sirve una sola vez.
            var otro = _api.Cliente();
            Assert.Equal(HttpStatusCode.Unauthorized, (await Ingresar(otro, correo, ClaveDefinitiva, null)).StatusCode);
            var codigo = Totp.Calcular(secreto, PasoActual() + 1);
            Assert.Equal(HttpStatusCode.OK, (await Ingresar(otro, correo, ClaveDefinitiva, codigo)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Ingresar(_api.Cliente(), correo, ClaveDefinitiva, codigo)).StatusCode);
        }

        await using var verificacion = baseDatos.CrearContexto();
        var gestor = await verificacion.Gestores.SingleAsync(g => g.Id == temporal.GestorId);
        Assert.False(gestor.DebeCambiarClave);
        Assert.DoesNotContain(temporal.Clave, gestor.HashClave);
        Assert.True(await verificacion.Actuaciones.AnyAsync(a => a.GestorId == gestor.Id && a.Accion == AccionesBitacora.RegistroTotp));
    }

    [Fact]
    public async Task Correo_inexistente_y_codigo_incorrecto_responden_igual_y_cinco_fallas_bloquean()
    {
        var cuenta = await CrearCuenta(RolGestor.Gestor);
        var cliente = _api.Cliente();

        static async Task<string> Cuerpo(HttpResponseMessage r)
        {
            using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            return $"{(int)r.StatusCode}|{json.RootElement.GetProperty("title")}|{json.RootElement.GetProperty("detail")}";
        }

        var inexistente = await Cuerpo(await Ingresar(cliente, "nadie@prueba.co", ClaveDefinitiva, "123456"));
        var codigoMalo = await Cuerpo(await Ingresar(cliente, cuenta.Correo, cuenta.Clave, "000000"));
        Assert.Equal(inexistente, codigoMalo);

        for (var i = 0; i < 4; i++)
        {
            await Ingresar(cliente, cuenta.Correo, "clave-equivocada", null);
        }

        var respuesta = await Ingresar(cliente, cuenta.Correo, cuenta.Clave, Totp.Calcular(cuenta.Secreto, PasoActual()));
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        await using var db = baseDatos.CrearContexto();
        Assert.True(await db.Gestores.AnyAsync(g => g.Id == cuenta.Id && g.BloqueadoHasta > DateTime.UtcNow));
        Assert.True(await db.Actuaciones.AnyAsync(a => a.GestorId == cuenta.Id && a.Accion == AccionesBitacora.GestorBloqueado));
    }

    [Fact]
    public async Task Un_gestor_solo_ve_sus_casos_y_cada_consulta_queda_en_la_bitacora()
    {
        var oficial = await Entrar(await CrearCuenta(RolGestor.OficialCumplimiento));
        var cuenta = await CrearCuenta(RolGestor.Gestor, "Gestora Asignada");
        var gestor = await Entrar(cuenta);
        var caso = await CrearDenuncia(new { nombre = "Ana Pérez", correo = "ana@prueba.co", telefono = "3001234567", relacion = "Empleado" });

        Assert.Contains(caso.Id, await IdsDeCasos(oficial));
        Assert.DoesNotContain(caso.Id, await IdsDeCasos(gestor));
        Assert.Equal(HttpStatusCode.NotFound, (await gestor.GetAsync($"/api/gestion/casos/{caso.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gestor.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/anotaciones", new { texto = "x" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await oficial.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/asignaciones", new { gestorId = cuenta.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await oficial.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/asignaciones", new { gestorId = cuenta.Id })).StatusCode);

        Assert.Contains(caso.Id, await IdsDeCasos(gestor));
        using var detalle = JsonDocument.Parse(await gestor.GetStringAsync($"/api/gestion/casos/{caso.Id}"));
        var raiz = detalle.RootElement;
        Assert.Equal("Relato confidencial del caso", raiz.GetProperty("relato").GetString());
        Assert.Equal("Oficina norte", raiz.GetProperty("lugar").GetString());
        Assert.Equal("Ana Pérez", raiz.GetProperty("identidad").GetProperty("nombre").GetString());
        Assert.Equal("Gestora Asignada", raiz.GetProperty("asignados")[0].GetProperty("nombre").GetString());
        var acciones = raiz.GetProperty("bitacora").EnumerateArray().Select(a => a.GetProperty("accion").GetString()).ToList();
        Assert.Equal(AccionesBitacora.ConsultaCaso, acciones[0]);
        Assert.Contains(AccionesBitacora.Asignacion, acciones);
        Assert.Contains(AccionesBitacora.DenunciaRecibida, acciones);

        // Al retirar la asignación pierde el acceso.
        Assert.Equal(HttpStatusCode.NoContent, (await oficial.DeleteAsync($"/api/gestion/casos/{caso.Id}/asignaciones/{cuenta.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gestor.GetAsync($"/api/gestion/casos/{caso.Id}")).StatusCode);
    }

    [Fact]
    public async Task Solo_el_oficial_asigna_y_administra_gestores()
    {
        var cuenta = await CrearCuenta(RolGestor.Gestor);
        var gestor = await Entrar(cuenta);
        var caso = await CrearDenuncia();

        Assert.Equal(HttpStatusCode.Forbidden,
            (await gestor.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/asignaciones", new { gestorId = cuenta.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.GetAsync("/api/gestion/gestores")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await gestor.PostAsJsonAsync("/api/gestion/gestores", new { nombre = "X", correo = "x@prueba.co", rol = "OficialCumplimiento" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Cliente().GetAsync("/api/gestion/casos")).StatusCode);
    }

    [Fact]
    public async Task La_respuesta_llega_al_buzon_del_denunciante_y_la_anotacion_no()
    {
        var oficial = await Entrar(await CrearCuenta(RolGestor.OficialCumplimiento));
        var caso = await CrearDenuncia();

        Assert.Equal(HttpStatusCode.NoContent,
            (await oficial.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/mensajes", new { texto = "Respuesta de la empresa" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await oficial.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/anotaciones", new { texto = "Nota interna reservada" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await oficial.PostAsJsonAsync($"/api/gestion/casos/{caso.Id}/anotaciones", new { texto = "  " })).StatusCode);

        var denunciante = _api.Cliente();
        await denunciante.PostAsJsonAsync("/api/publico/seguimiento/ingresar", new { codigo = caso.Codigo, clave = caso.Clave });
        var buzon = await denunciante.GetStringAsync("/api/publico/seguimiento");
        Assert.Contains("Respuesta de la empresa", buzon);
        Assert.DoesNotContain("Nota interna", buzon);

        await using var db = baseDatos.CrearContexto();
        var anotacion = await db.Anotaciones.SingleAsync(a => a.DenunciaId == caso.Id);
        Assert.DoesNotContain("Nota interna", Encoding.UTF8.GetString(anotacion.TextoCifrado));
        Assert.True(await db.Mensajes.AnyAsync(m => m.DenunciaId == caso.Id && m.Autor == AutorMensaje.Empresa && m.GestorId != null));
    }

    [Fact]
    public async Task Cerrar_exige_motivo_programa_la_purga_y_no_se_reabre()
    {
        var oficial = await Entrar(await CrearCuenta(RolGestor.OficialCumplimiento));
        var caso = await CrearDenuncia();
        var ruta = $"/api/gestion/casos/{caso.Id}/estado";

        Assert.Equal(HttpStatusCode.NoContent, (await oficial.PostAsJsonAsync(ruta, new { estado = "EnRevision" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await oficial.PostAsJsonAsync(ruta, new { estado = "EnRevision" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await oficial.PostAsJsonAsync(ruta, new { estado = "Cerrada" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await oficial.PostAsJsonAsync(ruta, new { estado = "Cerrada", motivoCierre = "NoFundada" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await oficial.PostAsJsonAsync(ruta, new { estado = "EnInvestigacion" })).StatusCode);

        await using var db = baseDatos.CrearContexto();
        var denuncia = await db.Denuncias.SingleAsync(d => d.Id == caso.Id);
        Assert.Equal(EstadoDenuncia.Cerrada, denuncia.Estado);
        Assert.Equal(MotivoCierre.NoFundada, denuncia.MotivoCierre);
        Assert.Equal(denuncia.CerradaEn!.Value.AddDays(365), denuncia.BorrarDatosEn);
        Assert.True(await db.Actuaciones.AnyAsync(a =>
            a.DenunciaId == caso.Id && a.Accion == AccionesBitacora.CambioEstado && a.Detalle == "EnRevision → Cerrada (NoFundada)"));
    }

    [Fact]
    public async Task Restablecer_el_TOTP_cierra_la_sesion_del_gestor()
    {
        var oficial = await Entrar(await CrearCuenta(RolGestor.OficialCumplimiento));
        var cuenta = await CrearCuenta(RolGestor.Gestor);
        var gestor = await Entrar(cuenta);
        Assert.Equal(HttpStatusCode.OK, (await gestor.GetAsync("/api/gestion/casos")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await oficial.PostAsync($"/api/gestion/gestores/{cuenta.Id}/restablecer-totp", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await gestor.GetAsync("/api/gestion/casos")).StatusCode);

        // Con la clave entra a una sesión incompleta para registrar el nuevo celular.
        var nueva = _api.Cliente();
        var ingreso = await Ingresar(nueva, cuenta.Correo, cuenta.Clave, null);
        Assert.False((await ingreso.Content.ReadFromJsonAsync<Sesion>())!.TotpConfirmado);
        Assert.Equal(HttpStatusCode.Forbidden, (await nueva.GetAsync("/api/gestion/casos")).StatusCode);
    }

    [Fact]
    public async Task Restablecer_la_clave_exige_cambiarla_y_el_oficial_no_se_restablece_a_si_mismo()
    {
        var cuentaOficial = await CrearCuenta(RolGestor.OficialCumplimiento);
        var oficial = await Entrar(cuentaOficial);
        var cuenta = await CrearCuenta(RolGestor.Gestor);

        var respuesta = await oficial.PostAsync($"/api/gestion/gestores/{cuenta.Id}/restablecer-clave", null);
        var temporal = (await respuesta.Content.ReadFromJsonAsync<Temporal>())!;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await oficial.PostAsync($"/api/gestion/gestores/{cuentaOficial.Id}/restablecer-clave", null)).StatusCode);

        var cliente = _api.Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Ingresar(cliente, cuenta.Correo, cuenta.Clave, Totp.Calcular(cuenta.Secreto, PasoActual()))).StatusCode);
        var ingreso = await Ingresar(cliente, cuenta.Correo, temporal.Clave, Totp.Calcular(cuenta.Secreto, PasoActual() + 1));
        var sesion = (await ingreso.Content.ReadFromJsonAsync<Sesion>())!;
        Assert.True(sesion.DebeCambiarClave);
        Assert.False(sesion.Completa);
    }

    [Fact]
    public async Task La_semilla_crea_el_primer_oficial_solo_si_no_hay_gestores()
    {
        var baseVacia = new BaseDatosPrueba();
        await baseVacia.InitializeAsync();
        try
        {
            var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Semilla:Oficial:Nombre"] = "Oficial Inicial",
                ["Semilla:Oficial:Correo"] = "Oficial@Prueba.co",
                ["Semilla:Oficial:Clave"] = "Clave-inicial-segura",
            }).Build();

            await using (var db = baseVacia.CrearContexto())
            {
                var servicio = new GestoresService(db, TimeProvider.System);
                Assert.True(await servicio.SembrarOficial(configuracion));
                Assert.False(await servicio.SembrarOficial(configuracion));
            }

            await using var verificacion = baseVacia.CrearContexto();
            var oficial = await verificacion.Gestores.SingleAsync();
            Assert.Equal("oficial@prueba.co", oficial.Correo);
            Assert.Equal(RolGestor.OficialCumplimiento, oficial.Rol);
            Assert.True(oficial.DebeCambiarClave);
            Assert.Null(oficial.TotpConfirmadoEn);
            Assert.True(Claves.Verificar("Clave-inicial-segura", oficial.HashClave));
        }
        finally
        {
            await baseVacia.DisposeAsync();
        }
    }
}
