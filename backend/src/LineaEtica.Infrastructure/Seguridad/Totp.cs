using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace LineaEtica.Infrastructure.Seguridad;

/// <summary>Códigos de un solo uso (TOTP, RFC 6238) compatibles con las aplicaciones de autenticación.</summary>
public static class Totp
{
    private const int SegundosPorPaso = 30;
    private const int Digitos = 6;
    private const int PasosDeTolerancia = 1;
    private const int TamanoSecreto = 20;
    private const string AlfabetoBase32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[] GenerarSecreto() => RandomNumberGenerator.GetBytes(TamanoSecreto);

    public static string UriDeRegistro(byte[] secreto, string correo, string emisor = "Línea ética LG")
    {
        var etiqueta = Uri.EscapeDataString($"{emisor}:{correo}");
        return $"otpauth://totp/{etiqueta}?secret={ABase32(secreto)}&issuer={Uri.EscapeDataString(emisor)}" +
               $"&algorithm=SHA1&digits={Digitos}&period={SegundosPorPaso}";
    }

    /// <summary>
    /// Devuelve el paso de tiempo del código si es válido, o null. Rechaza pasos ya usados, así que el llamador
    /// debe guardar el resultado en <c>Gestor.TotpUltimoPaso</c>: un mismo código no sirve dos veces.
    /// </summary>
    public static long? Verificar(byte[] secreto, string? codigo, DateTimeOffset ahora, long? ultimoPasoUsado)
    {
        if (codigo is not { Length: Digitos } || !codigo.All(char.IsAsciiDigit))
        {
            return null;
        }

        var pasoActual = ahora.ToUnixTimeSeconds() / SegundosPorPaso;
        var recibido = Encoding.ASCII.GetBytes(codigo);
        long? valido = null;

        // Revisa toda la ventana, sin salir antes, para no revelar cuál paso coincidió.
        for (var paso = pasoActual - PasosDeTolerancia; paso <= pasoActual + PasosDeTolerancia; paso++)
        {
            var esperado = Encoding.ASCII.GetBytes(Calcular(secreto, paso));
            if (CryptographicOperations.FixedTimeEquals(recibido, esperado) && paso > (ultimoPasoUsado ?? long.MinValue))
            {
                valido = paso;
            }
        }

        return valido;
    }

    internal static string Calcular(byte[] secreto, long paso)
    {
        Span<byte> contador = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(contador, paso);

        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(secreto, contador, hash);

        var desplazamiento = hash[^1] & 0x0F;
        var valor = BinaryPrimitives.ReadInt32BigEndian(hash.Slice(desplazamiento, 4)) & 0x7FFFFFFF;
        return (valor % 1_000_000).ToString("D6");
    }

    internal static string ABase32(byte[] datos)
    {
        var texto = new StringBuilder((datos.Length * 8 + 4) / 5);
        int acumulado = 0, bits = 0;
        foreach (var dato in datos)
        {
            acumulado = (acumulado << 8) | dato;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                texto.Append(AlfabetoBase32[(acumulado >> bits) & 31]);
            }
        }

        if (bits > 0)
        {
            texto.Append(AlfabetoBase32[(acumulado << (5 - bits)) & 31]);
        }

        return texto.ToString();
    }
}
