using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class TextoLegalConfiguracion : IEntityTypeConfiguration<TextoLegal>
{
    public void Configure(EntityTypeBuilder<TextoLegal> b)
    {
        b.ToTable("TextosLegales", t =>
        {
            t.HasTrigger("TR_TextosLegales_SoloInsertar");
            t.HasCheckConstraint("CK_TextosLegales_Tipo", "Tipo IN (1, 2, 3)");
        });
        b.HasKey(x => x.Id).HasName("PK_TextosLegales");
        b.HasAlternateKey(x => new { x.Tipo, x.Version }).HasName("UQ_TextosLegales_Tipo_Version");
        b.Property(x => x.VigenteDesde).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TextosLegales_Vigente");
        b.HasOne(x => x.PublicadoPor).WithMany().HasForeignKey(x => x.PublicadoPorId)
            .HasConstraintName("FK_TextosLegales_Gestores");
    }
}
