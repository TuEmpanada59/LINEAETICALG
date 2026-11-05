using LineaEtica.Infrastructure.Seguridad;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LineaEtica.Infrastructure;

public static class DependenciasInfraestructura
{
    public const string NombreConexion = "LineaEtica";

    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString(NombreConexion);
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{NombreConexion}'. " +
                "En desarrollo se configura con 'dotnet user-secrets'.");
        }

        servicios.AddDbContext<LineaEticaDbContext>(opciones =>
            opciones.UseSqlServer(cadena, sql => sql.EnableRetryOnFailure()));

        var opcionesCifrado = configuracion.GetSection(OpcionesCifrado.Seccion).Get<OpcionesCifrado>() ?? new OpcionesCifrado();
        servicios.AddSingleton(new CifradoDatos(opcionesCifrado));

        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<ParametrosCanal>();
        servicios.AddScoped<DenunciaService>();
        servicios.AddScoped<SeguimientoService>();
        servicios.AddScoped<AccesoGestorService>();
        servicios.AddScoped<CasosService>();
        servicios.AddScoped<GestoresService>();
        servicios.AddScoped<PurgaDatosService>();

        return servicios;
    }
}
