using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class AsignacionConfiguracion : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> b)
    {
        b.ToTable("Asignaciones");
        b.HasKey(x => new { x.DenunciaId, x.GestorId }).HasName("PK_Asignaciones");
        b.Property(x => x.AsignadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Asignaciones_En");
        b.HasOne(x => x.Denuncia).WithMany(x => x.Asignaciones).HasForeignKey(x => x.DenunciaId)
            .HasConstraintName("FK_Asignaciones_Denuncias");
        b.HasOne(x => x.Gestor).WithMany().HasForeignKey(x => x.GestorId)
            .HasConstraintName("FK_Asignaciones_Gestores");
        b.HasOne(x => x.AsignadaPor).WithMany().HasForeignKey(x => x.AsignadaPorId)
            .HasConstraintName("FK_Asignaciones_AsignadaPor");
        b.HasIndex(x => x.GestorId).HasDatabaseName("IX_Asignaciones_Gestor");
    }
}
