using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class MensajeConfiguracion : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> b)
    {
        b.ToTable("Mensajes", t => t.HasCheckConstraint("CK_Mensajes_Autor",
            "(Autor = 1 AND GestorId IS NULL) OR (Autor = 2 AND GestorId IS NOT NULL)"));
        b.HasKey(x => x.Id).HasName("PK_Mensajes");
        b.Property(x => x.CreadoEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Mensajes_CreadoEn");
        b.HasOne(x => x.Denuncia).WithMany(x => x.Mensajes).HasForeignKey(x => x.DenunciaId)
            .HasConstraintName("FK_Mensajes_Denuncias");
        b.HasOne(x => x.Gestor).WithMany().HasForeignKey(x => x.GestorId)
            .HasConstraintName("FK_Mensajes_Gestores");
        b.HasIndex(x => new { x.DenunciaId, x.CreadoEn }).HasDatabaseName("IX_Mensajes_Denuncia");
    }
}
