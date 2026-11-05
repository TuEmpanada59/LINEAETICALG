namespace LineaEtica.Infrastructure.Servicios;

internal static class Reloj
{
    /// <summary>Hora UTC sin fracciones de segundo, igual a como la guarda la base (datetime2(0)).</summary>
    public static DateTime Ahora(this TimeProvider reloj)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
