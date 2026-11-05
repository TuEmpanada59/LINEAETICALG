using LineaEtica.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LineaEtica.Tests;

/// <summary>Base temporal en LocalDB, creada con las migraciones reales y borrada al terminar.</summary>
public sealed class BaseDatosPrueba : IAsyncLifetime
{
    public string Cadena { get; } =
        $@"Server=(localdb)\MSSQLLocalDB;Database=LineaEtica_Prueba_{Guid.NewGuid():N};Integrated Security=True;";

    public LineaEticaDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<LineaEticaDbContext>().UseSqlServer(Cadena).Options);

    public async Task InitializeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE USER usuario_api_prueba WITHOUT LOGIN; ALTER ROLE rol_lineaetica_api ADD MEMBER usuario_api_prueba;");
    }

    public async Task DisposeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BaseDatosPrueba>
{
    public const string Nombre = "Base de datos";
}
