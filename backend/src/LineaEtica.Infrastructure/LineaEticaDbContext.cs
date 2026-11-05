using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LineaEtica.Infrastructure;

public class LineaEticaDbContext(DbContextOptions<LineaEticaDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Gestor> Gestores => Set<Gestor>();
    public DbSet<Denuncia> Denuncias => Set<Denuncia>();
    public DbSet<Identidad> Identidades => Set<Identidad>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<Anotacion> Anotaciones => Set<Anotacion>();
    public DbSet<Actuacion> Actuaciones => Set<Actuacion>();
    public DbSet<ParametroConfiguracion> Configuracion => Set<ParametroConfiguracion>();
    public DbSet<TextoLegal> TextosLegales => Set<TextoLegal>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion)
    {
        // Los índices los define el esquema de forma explícita; EF no debe agregar uno por cada llave foránea.
        configuracion.Conventions.Remove<ForeignKeyIndexConvention>();
        // Todas las fechas se guardan en UTC; al leerlas se marcan como UTC para que el navegador no las tome por locales.
        configuracion.Properties<DateTime>().HavePrecision(0).HaveConversion<ConversorUtc>();
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.ApplyConfigurationsFromAssembly(typeof(LineaEticaDbContext).Assembly);

        // Ninguna cascada: la purga borra las filas de forma explícita y una denuncia nunca se elimina.
        foreach (var llave in modelo.Model.GetEntityTypes().SelectMany(entidad => entidad.GetForeignKeys()))
        {
            llave.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}

internal sealed class ConversorUtc() : ValueConverter<DateTime, DateTime>(
    valor => valor,
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
