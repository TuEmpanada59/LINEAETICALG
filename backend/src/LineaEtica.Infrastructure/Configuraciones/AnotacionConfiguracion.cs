using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class AnotacionConfiguracion : IEntityTypeConfiguration<Anotacion>
{
    public void Configure(EntityTypeBuilder<Anotacion> b)
    {
        b.ToTable("Anotaciones");
        b.HasKey(x => x.Id).HasName("PK_Anotaciones");
        b.Property(x => x.CreadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Anotaciones_CreadaEn");
        b.HasOne(x => x.Denuncia).WithMany(x => x.Anotaciones).HasForeignKey(x => x.DenunciaId)
            .HasConstraintName("FK_Anotaciones_Denuncias");
        b.HasOne(x => x.Gestor).WithMany().HasForeignKey(x => x.GestorId)
            .HasConstraintName("FK_Anotaciones_Gestores");
        b.HasIndex(x => new { x.DenunciaId, x.CreadaEn }).HasDatabaseName("IX_Anotaciones_Denuncia");
    }
}
