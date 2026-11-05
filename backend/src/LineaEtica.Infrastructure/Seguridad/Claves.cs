using System.Security.Cryptography;

namespace LineaEtica.Infrastructure.Seguridad;

/// <summary>Hash PBKDF2-SHA256 de contraseñas y generación de códigos de seguimiento.</summary>
public static class Claves
{
    private const string Prefijo = "pbkdf2-sha256";
    private const int Iteraciones = 600_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    // Sin 0, O, 1 ni I para que el denunciante no los confunda al copiarlos.
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static readonly Lazy<string> HashFicticio = new(() => Hash(Guid.NewGuid().ToString()));

    public static string Hash(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string clave, string? hashGuardado)
    {
        var partes = hashGuardado?.Split('$');
        if (partes is not { Length: 4 } || partes[0] != Prefijo || !int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0)
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (Exception error) when (error is FormatException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gasta el mismo tiempo que una verificación real. Se usa cuando el código o el correo no existen,
    /// para que el tiempo de respuesta no revele si existen.
    /// </summary>
    public static void SimularVerificacion(string clave) => Verificar(clave, HashFicticio.Value);

    /// <summary>Código público del caso: LG-XXXX-XXXX.</summary>
    public static string GenerarCodigo() => $"LG-{Bloque(4)}-{Bloque(4)}";

    /// <summary>Contraseña de seguimiento del denunciante: 16 caracteres aleatorios (80 bits).</summary>
    public static string GenerarClaveSeguimiento() => $"{Bloque(4)}-{Bloque(4)}-{Bloque(4)}-{Bloque(4)}";

    /// <summary>Contraseña temporal de un gestor (80 bits); debe cambiarla en su primer ingreso.</summary>
    public static string GenerarClaveTemporal() => GenerarClaveSeguimiento();

    private static string Bloque(int tamano) => RandomNumberGenerator.GetString(Alfabeto, tamano);
}
