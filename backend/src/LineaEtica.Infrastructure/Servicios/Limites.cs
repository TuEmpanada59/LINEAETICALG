namespace LineaEtica.Infrastructure.Servicios;

/// <summary>
/// Longitud máxima de cada texto, en caracteres. Las de los campos cifrados con columna de tamaño fijo
/// están calculadas para que el valor cifrado quepa aunque todos los caracteres ocupen 4 bytes.
/// </summary>
public static class Limites
{
    public const int Relato = 20_000;
    public const int Mensaje = 5_000;
    public const int FechaHechos = 40;
    public const int Lugar = 240;
    public const int PersonasInvolucradas = 2_000;
    public const int Nombre = 200;
    public const int Correo = 240;
    public const int Telefono = 30;
}
