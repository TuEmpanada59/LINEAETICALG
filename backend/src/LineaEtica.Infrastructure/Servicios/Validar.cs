using System.Net.Mail;
using LineaEtica.Domain;

namespace LineaEtica.Infrastructure.Servicios;

internal static class Validar
{
    public static string Requerido(string? valor, string campo, int maximo) =>
        Opcional(valor, campo, maximo) ?? throw new ErrorValidacion($"El campo «{campo}» es obligatorio.");

    public static string? Opcional(string? valor, string campo, int maximo)
    {
        var limpio = valor?.Trim();
        if (string.IsNullOrEmpty(limpio))
        {
            return null;
        }

        return limpio.Length <= maximo
            ? limpio
            : throw new ErrorValidacion($"El campo «{campo}» admite máximo {maximo} caracteres.");
    }

    public static string Correo(string? valor)
    {
        var correo = Requerido(valor, "correo", Limites.Correo);
        return MailAddress.TryCreate(correo, out var direccion) && direccion.Address == correo
            ? correo
            : throw new ErrorValidacion("El correo no es válido.");
    }

    public static string? Telefono(string? valor)
    {
        var telefono = Opcional(valor, "teléfono", Limites.Telefono);
        return telefono is null || telefono.All(c => char.IsAsciiDigit(c) || c is ' ' or '+' or '-' or '(' or ')')
            ? telefono
            : throw new ErrorValidacion("El teléfono solo admite números, espacios y los signos + - ( ).");
    }

    public static T Opcion<T>(T valor, string campo) where T : struct, Enum =>
        Enum.IsDefined(valor) ? valor : throw new ErrorValidacion($"Seleccione una opción válida en «{campo}».");
}
