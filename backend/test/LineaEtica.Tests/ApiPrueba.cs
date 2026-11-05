using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LineaEtica.Tests;

/// <summary>
/// La API completa en memoria, contra la base temporal de LocalDB. Nunca usa los secretos locales.
/// La purga automática está apagada y los límites por IP son altos, salvo que la prueba los cambie.
/// </summary>
public sealed class ApiPrueba(BaseDatosPrueba baseDatos, IReadOnlyDictionary<string, string>? ajustes = null)
    : WebApplicationFactory<Program>
{
    public static readonly string Llave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.UseSetting("ConnectionStrings:LineaEtica", baseDatos.Cadena);
        builder.UseSetting("Cifrado:Llave", Llave);
        builder.UseSetting("Purga:Habilitada", "false");
        builder.UseSetting("LimitesIp:Ingresos", "100000");
        builder.UseSetting("LimitesIp:Denuncias", "100000");

        foreach (var (clave, valor) in ajustes ?? new Dictionary<string, string>())
        {
            builder.UseSetting(clave, valor);
        }
    }

    public HttpClient Cliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });
}
