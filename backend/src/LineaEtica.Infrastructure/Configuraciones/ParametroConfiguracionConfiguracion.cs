using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class ParametroConfiguracionConfiguracion : IEntityTypeConfiguration<ParametroConfiguracion>
{
    public void Configure(EntityTypeBuilder<ParametroConfiguracion> b)
    {
        b.ToTable("Configuracion");
        b.HasKey(x => x.Clave).HasName("PK_Configuracion");
        b.Property(x => x.Clave).HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.Descripcion).HasMaxLength(300);
        b.Property(x => x.ModificadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Configuracion_En");
        b.HasOne(x => x.ModificadaPor).WithMany().HasForeignKey(x => x.ModificadaPorId)
            .HasConstraintName("FK_Configuracion_Gestores");
    }
}
