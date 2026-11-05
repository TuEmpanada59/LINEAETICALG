using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LineaEtica.Infrastructure.Seguridad;

namespace LineaEtica.Tests;

public class CifradoDatosPruebas
{
    private static string Llave() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static CifradoDatos Cifrado(string? llave = null) => new(new OpcionesCifrado { Llave = llave ?? Llave() });

    [Theory]
    [InlineData("Relato con tildes y ñ: acción, información")]
    [InlineData("")]
    public void Descifra_lo_que_cifro(string texto)
    {
        var cifrado = Cifrado();

        Assert.Equal(texto, cifrado.Descifrar(cifrado.Cifrar(texto, "ctx"), "ctx"));
    }

    [Fact]
    public void El_resultado_no_contiene_el_texto_y_cambia_en_cada_cifrado()
    {
        var cifrado = Cifrado();
        var primero = cifrado.Cifrar("texto secreto", "ctx");
        var segundo = cifrado.Cifrar("texto secreto", "ctx");

        Assert.NotEqual(primero, segundo);
        Assert.DoesNotContain("texto secreto", Encoding.UTF8.GetString(primero));
    }

    [Fact]
    public void Rechaza_un_dato_alterado()
    {
        var cifrado = Cifrado();
        var datos = cifrado.Cifrar("texto", "ctx");
        datos[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => cifrado.Descifrar(datos, "ctx"));
    }

    [Fact]
    public void Un_valor_copiado_a_otro_caso_no_se_puede_leer()
    {
        var cifrado = Cifrado();
        var datos = cifrado.Cifrar("relato", ContextoCifrado.Relato(Guid.NewGuid()));

        Assert.ThrowsAny<CryptographicException>(() => cifrado.Descifrar(datos, ContextoCifrado.Relato(Guid.NewGuid())));
    }

    [Fact]
    public void Un_valor_copiado_a_otro_campo_no_se_puede_leer()
    {
        var cifrado = Cifrado();
        var id = Guid.NewGuid();
        var datos = cifrado.Cifrar("relato", ContextoCifrado.Relato(id));

        Assert.ThrowsAny<CryptographicException>(() => cifrado.Descifrar(datos, ContextoCifrado.Lugar(id)));
    }

    [Fact]
    public void Otra_llave_no_puede_descifrar()
    {
        var datos = Cifrado().Cifrar("texto", "ctx");

        Assert.ThrowsAny<CryptographicException>(() => Cifrado().Descifrar(datos, "ctx"));
    }

    [Fact]
    public void Tras_rotar_la_llave_sigue_leyendo_lo_anterior_y_cifra_con_la_nueva()
    {
        var llaveVieja = Llave();
        var datosViejos = Cifrado(llaveVieja).Cifrar("antes", "ctx");

        var rotado = new CifradoDatos(new OpcionesCifrado
        {
            Llave = Llave(),
            Version = 2,
            LlavesAnteriores = new() { [1] = llaveVieja },
        });

        Assert.Equal("antes", rotado.Descifrar(datosViejos, "ctx"));
        Assert.Equal(2, rotado.Cifrar("despues", "ctx")[0]);
    }

    [Fact]
    public void Rechaza_un_dato_demasiado_corto()
    {
        Assert.ThrowsAny<CryptographicException>(() => Cifrado().Descifrar([1, 2, 3], "ctx"));
    }

    [Theory]
    [InlineData("esto no es base64!")]
    [InlineData("AAAA")]
    public void Rechaza_una_llave_invalida(string llave)
    {
        Assert.Throws<InvalidOperationException>(() => Cifrado(llave));
    }
}

public class ClavesPruebas
{
    [Fact]
    public void Verifica_la_clave_correcta_y_rechaza_otra()
    {
        var hash = Claves.Hash("Mi-Clave-Correcta");

        Assert.True(Claves.Verificar("Mi-Clave-Correcta", hash));
        Assert.False(Claves.Verificar("mi-clave-correcta", hash));
    }

    [Fact]
    public void Dos_hashes_de_la_misma_clave_son_distintos_y_no_la_contienen()
    {
        var primero = Claves.Hash("Mi-Clave");
        var segundo = Claves.Hash("Mi-Clave");

        Assert.NotEqual(primero, segundo);
        Assert.DoesNotContain("Mi-Clave", primero);
        Assert.StartsWith("pbkdf2-sha256$600000$", primero);
    }

    [Fact]
    public void El_hash_cabe_en_la_columna()
    {
        Assert.True(Claves.Hash("clave").Length <= 200);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("texto-cualquiera")]
    [InlineData("pbkdf2-sha256$abc$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$no-base64!$AAAA")]
    [InlineData("otro$1000$AAAA$AAAA")]
    public void Un_hash_mal_formado_o_purgado_no_verifica(string? hash)
    {
        Assert.False(Claves.Verificar("clave", hash));
    }

    [Fact]
    public void El_codigo_tiene_el_formato_acordado_y_sin_caracteres_ambiguos()
    {
        for (var i = 0; i < 200; i++)
        {
            Assert.Matches(new Regex("^LG-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$"), Claves.GenerarCodigo());
        }
    }

    [Fact]
    public void La_clave_de_seguimiento_es_larga_y_no_se_repite()
    {
        var claves = Enumerable.Range(0, 200).Select(_ => Claves.GenerarClaveSeguimiento()).ToList();

        Assert.All(claves, clave => Assert.Matches(new Regex("^([A-HJ-NP-Z2-9]{4}-){3}[A-HJ-NP-Z2-9]{4}$"), clave));
        Assert.Equal(claves.Count, claves.Distinct().Count());
    }
}

public class TotpPruebas
{
    // Secreto y resultados de los vectores de prueba del RFC 6238, recortados a 6 dígitos.
    private static readonly byte[] SecretoRfc = Encoding.ASCII.GetBytes("12345678901234567890");

    private static DateTimeOffset Momento(long segundos) => DateTimeOffset.FromUnixTimeSeconds(segundos);

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void Coincide_con_los_vectores_del_rfc(long segundos, string esperado)
    {
        Assert.Equal(esperado, Totp.Calcular(SecretoRfc, segundos / 30));
    }

    [Fact]
    public void Acepta_el_codigo_vigente_y_devuelve_su_paso()
    {
        Assert.Equal(59 / 30, Totp.Verificar(SecretoRfc, "287082", Momento(59), null));
    }

    [Fact]
    public void Tolera_un_paso_de_desfase_pero_no_mas()
    {
        Assert.NotNull(Totp.Verificar(SecretoRfc, "287082", Momento(59 + 30), null));
        Assert.NotNull(Totp.Verificar(SecretoRfc, "287082", Momento(59 - 30), null));
        Assert.Null(Totp.Verificar(SecretoRfc, "287082", Momento(59 + 60), null));
    }

    [Fact]
    public void Un_codigo_no_se_puede_usar_dos_veces()
    {
        var paso = Totp.Verificar(SecretoRfc, "287082", Momento(59), null);

        Assert.Null(Totp.Verificar(SecretoRfc, "287082", Momento(59), paso));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("000000")]
    public void Rechaza_codigos_invalidos(string? codigo)
    {
        Assert.Null(Totp.Verificar(SecretoRfc, codigo, Momento(59), null));
    }

    [Fact]
    public void Base32_coincide_con_el_rfc_4648()
    {
        Assert.Equal("GEZDGNBVGY3TQOJQ", Totp.ABase32(Encoding.ASCII.GetBytes("1234567890")));
    }

    [Fact]
    public void La_uri_de_registro_no_revela_datos_de_mas_y_lleva_el_secreto()
    {
        var uri = Totp.UriDeRegistro(Encoding.ASCII.GetBytes("1234567890"), "gestor@empresa.co");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("secret=GEZDGNBVGY3TQOJQ", uri);
        Assert.Contains("digits=6", uri);
    }
}
