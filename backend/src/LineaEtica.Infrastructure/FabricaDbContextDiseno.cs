using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LineaEtica.Infrastructure;

/// <summary>Solo para las herramientas de EF: genera migraciones sin conectarse ni leer secretos.</summary>
public class FabricaDbContextDiseno : IDesignTimeDbContextFactory<LineaEticaDbContext>
{
    public LineaEticaDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<LineaEticaDbContext>()
            .UseSqlServer("Server=sin-conexion;Database=sin-conexion;")
            .Options;

        return new LineaEticaDbContext(opciones);
    }
}
