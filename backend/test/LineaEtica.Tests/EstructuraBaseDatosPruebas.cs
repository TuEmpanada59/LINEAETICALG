using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Tests;

[Collection(ColeccionBaseDatos.Nombre)]
public class EstructuraBaseDatosPruebas(BaseDatosPrueba baseDatos)
{
    private static readonly byte[] Cifrado = [1, 2, 3];

    private static async Task<Denuncia> NuevaDenuncia(Infrastructure.LineaEticaDbContext db, ModoDenuncia modo)
    {
        var aviso = await db.TextosLegales.FirstAsync(t => t.Tipo == TipoTextoLegal.AvisoPrivacidad);
        var autorizacion = await db.TextosLegales.FirstAsync(t => t.Tipo == TipoTextoLegal.Autorizacion);
        var empresa = await db.Empresas.FirstAsync();

        return new Denuncia
        {
            Codigo = $"LG-{Guid.NewGuid():N}"[..12],
            HashClave = "hash",
            EmpresaId = empresa.Id,
            Modo = modo,
            Categoria = CategoriaConducta.Fraude,
            RelatoCifrado = Cifrado,
            AceptoTratamientoEn = DateTime.UtcNow,
            TextoAvisoId = aviso.Id,
            TextoAutorizacionId = autorizacion.Id,
        };
    }

    [Fact]
    public async Task El_modelo_no_tiene_cambios_sin_migrar()
    {
        await using var db = baseDatos.CrearContexto();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task La_migracion_carga_los_datos_iniciales()
    {
        await using var db = baseDatos.CrearContexto();

        Assert.True(await db.Empresas.AnyAsync(e => e.Nombre == "Londoño Gómez"));
        Assert.True(await db.Configuracion.AnyAsync(c => c.Clave == ParametroConfiguracion.TextoAcuse));
        Assert.True(await db.Configuracion.AnyAsync(c => c.Clave == ParametroConfiguracion.PlazoRespuestaDias));
        Assert.True(await db.Configuracion.AnyAsync(c => c.Clave == ParametroConfiguracion.RetencionDiasTrasCierre));
        Assert.Equal(3, await db.TextosLegales.Where(t => t.Version == 1).Select(t => t.Tipo).Distinct().CountAsync());
    }

    [Fact]
    public async Task Guarda_una_denuncia_a_nombre_propio_con_su_identidad()
    {
        await using var db = baseDatos.CrearContexto();
        var denuncia = await NuevaDenuncia(db, ModoDenuncia.ANombrePropio);
        denuncia.Identidad = new Identidad
        {
            NombreCifrado = Cifrado,
            CorreoCifrado = Cifrado,
            Relacion = RelacionDenunciante.Empleado,
        };
        db.Denuncias.Add(denuncia);

        await db.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, denuncia.Id);
        Assert.NotEmpty(denuncia.Version);
        Assert.NotEqual(default, denuncia.CreadaEn);
        Assert.Equal(EstadoDenuncia.Recibida, denuncia.Estado);
        Assert.Equal(denuncia.Id, denuncia.Identidad.DenunciaId);
    }

    [Fact]
    public async Task Una_denuncia_anonima_no_admite_identidad()
    {
        await using var db = baseDatos.CrearContexto();
        var denuncia = await NuevaDenuncia(db, ModoDenuncia.Anonimo);
        db.Denuncias.Add(denuncia);
        await db.SaveChangesAsync();

        await using var otro = baseDatos.CrearContexto();
        var error = await Assert.ThrowsAsync<SqlException>(() => otro.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT dbo.Identidades (DenunciaId, NombreCifrado, CorreoCifrado, Relacion) VALUES ({denuncia.Id}, {Cifrado}, {Cifrado}, 1)"));

        Assert.Contains("FK_Identidades_Denuncias", error.Message);
    }

    [Fact]
    public async Task La_bitacora_admite_insertar_con_EF_pero_no_modificar()
    {
        await using var db = baseDatos.CrearContexto();
        var actuacion = new Actuacion { ActorTipo = TipoActor.Sistema, Accion = "Prueba" };
        db.Actuaciones.Add(actuacion);
        await db.SaveChangesAsync();
        Assert.True(actuacion.Id > 0);

        actuacion.Accion = "Cambiada";
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Un_texto_legal_admite_version_nueva_pero_no_edicion()
    {
        await using var db = baseDatos.CrearContexto();
        var version = await db.TextosLegales
            .Where(t => t.Tipo == TipoTextoLegal.PoliticaNoRepresalias)
            .MaxAsync(t => t.Version) + 1;
        var texto = new TextoLegal { Tipo = TipoTextoLegal.PoliticaNoRepresalias, Version = version, Contenido = "v" };
        db.TextosLegales.Add(texto);
        await db.SaveChangesAsync();
        Assert.True(texto.Id > 0);

        texto.Contenido = "Editado";
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("TRUNCATE TABLE dbo.Actuaciones")]
    [InlineData("DELETE dbo.Actuaciones")]
    [InlineData("UPDATE dbo.Actuaciones SET Accion = N'X'")]
    [InlineData("DELETE dbo.Denuncias")]
    [InlineData("DROP TABLE dbo.Anotaciones")]
    [InlineData("DISABLE TRIGGER dbo.TR_Actuaciones_SoloInsertar ON dbo.Actuaciones")]
    public async Task El_rol_de_la_API_no_puede_alterar_lo_protegido(string sentencia)
    {
        await using var db = baseDatos.CrearContexto();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("EXECUTE AS USER = 'usuario_api_prueba';");

        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sentencia));

        await db.Database.ExecuteSqlRawAsync("REVERT;");
    }

    [Fact]
    public async Task El_rol_de_la_API_puede_operar_y_escribir_en_la_bitacora()
    {
        await using var db = baseDatos.CrearContexto();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("EXECUTE AS USER = 'usuario_api_prueba';");

        var denuncia = await NuevaDenuncia(db, ModoDenuncia.Anonimo);
        db.Denuncias.Add(denuncia);
        db.Actuaciones.Add(new Actuacion { ActorTipo = TipoActor.Sistema, Accion = "Prueba con rol" });
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("REVERT;");
    }
}
