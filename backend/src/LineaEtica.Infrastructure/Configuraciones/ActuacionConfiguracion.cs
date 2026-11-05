using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class ActuacionConfiguracion : IEntityTypeConfiguration<Actuacion>
{
    public void Configure(EntityTypeBuilder<Actuacion> b)
    {
        b.ToTable("Actuaciones", t =>
        {
            t.HasTrigger("TR_Actuaciones_SoloInsertar");
            t.HasCheckConstraint("CK_Actuaciones_Actor",
                "(ActorTipo = 1 AND GestorId IS NULL AND DenunciaId IS NOT NULL) " +
                "OR (ActorTipo = 2 AND GestorId IS NOT NULL) " +
                "OR (ActorTipo = 3 AND GestorId IS NULL)");
        });
        b.HasKey(x => x.Id).HasName("PK_Actuaciones");
        b.Property(x => x.Accion).HasMaxLength(100);
        b.Property(x => x.Detalle).HasMaxLength(1000);
        b.Property(x => x.CreadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Actuaciones_CreadaEn");
        b.HasOne(x => x.Denuncia).WithMany().HasForeignKey(x => x.DenunciaId)
            .HasConstraintName("FK_Actuaciones_Denuncias");
        b.HasOne(x => x.Gestor).WithMany().HasForeignKey(x => x.GestorId)
            .HasConstraintName("FK_Actuaciones_Gestores");
        b.HasIndex(x => new { x.DenunciaId, x.CreadaEn }).HasDatabaseName("IX_Actuaciones_Denuncia");
    }
}
