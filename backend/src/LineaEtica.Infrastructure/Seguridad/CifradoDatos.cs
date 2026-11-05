using System.Security.Cryptography;
using System.Text;

namespace LineaEtica.Infrastructure.Seguridad;

public sealed class OpcionesCifrado
{
    public const string Seccion = "Cifrado";

    /// <summary>Llave actual: AES-256 en base64 (32 bytes).</summary>
    public string? Llave { get; set; }

    /// <summary>Versión de la llave actual. Se incrementa al rotarla.</summary>
    public byte Version { get; set; } = 1;

    /// <summary>Llaves anteriores por versión, para seguir leyendo lo ya cifrado con ellas.</summary>
    public Dictionary<byte, string> LlavesAnteriores { get; set; } = [];
}

/// <summary>
/// Cifrado AES-256-GCM. Formato: [versión de llave: 1][nonce: 12][etiqueta: 16][texto cifrado].
/// El contexto ata cada valor a su caso y campo: un valor copiado a otro lugar no se puede descifrar.
/// </summary>
public sealed class CifradoDatos
{
    private const int TamanoLlave = 32;
    private const int TamanoNonce = 12;
    private const int TamanoEtiqueta = 16;
    private const int TamanoEncabezado = 1 + TamanoNonce + TamanoEtiqueta;

    private readonly byte _versionActual;
    private readonly Dictionary<byte, byte[]> _llaves = [];

    public CifradoDatos(OpcionesCifrado opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.Llave))
        {
            throw new InvalidOperationException(
                $"Falta la llave '{OpcionesCifrado.Seccion}:Llave'. Se genera con scripts/generar-llave.ps1.");
        }

        _versionActual = opciones.Version;
        _llaves[opciones.Version] = LeerLlave(opciones.Llave, opciones.Version);
        foreach (var (version, llave) in opciones.LlavesAnteriores)
        {
            _llaves.Add(version, LeerLlave(llave, version));
        }
    }

    public byte[] Cifrar(string texto, string contexto)
    {
        var plano = Encoding.UTF8.GetBytes(texto);
        var resultado = new byte[TamanoEncabezado + plano.Length];
        var destino = resultado.AsSpan();

        destino[0] = _versionActual;
        var nonce = destino.Slice(1, TamanoNonce);
        var etiqueta = destino.Slice(1 + TamanoNonce, TamanoEtiqueta);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_llaves[_versionActual], TamanoEtiqueta);
        aes.Encrypt(nonce, plano, destino[TamanoEncabezado..], etiqueta, Encoding.UTF8.GetBytes(contexto));
        return resultado;
    }

    /// <exception cref="CryptographicException">Si el dato fue alterado, no corresponde al contexto o no hay llave.</exception>
    public string Descifrar(byte[] datos, string contexto)
    {
        if (datos.Length < TamanoEncabezado)
        {
            throw new CryptographicException("El dato cifrado no tiene el formato esperado.");
        }

        var origen = datos.AsSpan();
        if (!_llaves.TryGetValue(origen[0], out var llave))
        {
            throw new CryptographicException($"No hay llave para la versión {origen[0]}.");
        }

        var plano = new byte[datos.Length - TamanoEncabezado];
        using var aes = new AesGcm(llave, TamanoEtiqueta);
        aes.Decrypt(
            origen.Slice(1, TamanoNonce),
            origen[TamanoEncabezado..],
            origen.Slice(1 + TamanoNonce, TamanoEtiqueta),
            plano,
            Encoding.UTF8.GetBytes(contexto));
        return Encoding.UTF8.GetString(plano);
    }

    private static byte[] LeerLlave(string base64, byte version)
    {
        byte[] llave;
        try
        {
            llave = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"La llave de cifrado versión {version} no está en base64.");
        }

        return llave.Length == TamanoLlave
            ? llave
            : throw new InvalidOperationException($"La llave de cifrado versión {version} debe tener {TamanoLlave} bytes.");
    }
}

/// <summary>Contextos de cifrado de cada campo. Cambiar uno deja ilegible lo ya guardado.</summary>
public static class ContextoCifrado
{
    public static string Relato(Guid denunciaId) => $"denuncia/{denunciaId:N}/relato";
    public static string FechaHechos(Guid denunciaId) => $"denuncia/{denunciaId:N}/fecha-hechos";
    public static string Lugar(Guid denunciaId) => $"denuncia/{denunciaId:N}/lugar";
    public static string PersonasInvolucradas(Guid denunciaId) => $"denuncia/{denunciaId:N}/personas";
    public static string Nombre(Guid denunciaId) => $"denuncia/{denunciaId:N}/nombre";
    public static string Correo(Guid denunciaId) => $"denuncia/{denunciaId:N}/correo";
    public static string Telefono(Guid denunciaId) => $"denuncia/{denunciaId:N}/telefono";
    public static string Mensaje(Guid denunciaId) => $"denuncia/{denunciaId:N}/mensaje";
    public static string Anotacion(Guid denunciaId) => $"denuncia/{denunciaId:N}/anotacion";
    public static string SecretoTotp(int gestorId) => $"gestor/{gestorId}/totp";
}
