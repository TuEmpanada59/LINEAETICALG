using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LineaEtica.Tests;

/// <summary>La API completa en memoria, contra la base temporal de LocalDB. Nunca usa los secretos locales.</summary>
public sealed class ApiPrueba(BaseDatosPrueba baseDatos) : WebApplicationFactory<Program>
{
    public static readonly string Llave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.UseSetting("ConnectionStrings:LineaEtica", baseDatos.Cadena);
        builder.UseSetting("Cifrado:Llave", Llave);
    }

    public HttpClient Cliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });
}
