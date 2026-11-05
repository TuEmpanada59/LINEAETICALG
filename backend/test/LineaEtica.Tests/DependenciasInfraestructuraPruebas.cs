using LineaEtica.Infrastructure;
using LineaEtica.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LineaEtica.Tests;

public class DependenciasInfraestructuraPruebas
{
    private const string CadenaValida = "Server=servidor-de-prueba;Database=BaseDePrueba;";
    private static readonly string LlaveValida = Convert.ToBase64String(new byte[32]);

    private static IConfiguration Configuracion(string? cadena, string? llave) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LineaEtica"] = cadena,
                ["Cifrado:Llave"] = llave,
            })
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_cadena_de_conexion_falla_con_un_mensaje_claro(string? cadena)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AgregarInfraestructura(Configuracion(cadena, LlaveValida)));

        Assert.Contains("ConnectionStrings:LineaEtica", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sin_llave_de_cifrado_falla_con_un_mensaje_claro(string? llave)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AgregarInfraestructura(Configuracion(CadenaValida, llave)));

        Assert.Contains("Cifrado:Llave", error.Message);
    }

    [Fact]
    public void Con_la_configuracion_completa_usa_sql_server_y_registra_el_cifrado()
    {
        using var proveedor = new ServiceCollection()
            .AgregarInfraestructura(Configuracion(CadenaValida, LlaveValida))
            .BuildServiceProvider();
        using var alcance = proveedor.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<LineaEticaDbContext>();

        Assert.True(db.Database.IsSqlServer());
        Assert.Equal("BaseDePrueba", db.Database.GetDbConnection().Database);
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<CifradoDatos>());
    }
}
