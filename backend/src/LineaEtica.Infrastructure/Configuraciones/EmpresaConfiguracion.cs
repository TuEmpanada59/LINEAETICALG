using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("Empresas");
        b.HasKey(x => x.Id).HasName("PK_Empresas");
        b.HasAlternateKey(x => x.Nombre).HasName("UQ_Empresas_Nombre");
        b.Property(x => x.Nombre).HasMaxLength(200);
        b.Property(x => x.Activa).HasDefaultValue(true, "DF_Empresas_Activa").ValueGeneratedNever();
        b.Property(x => x.CreadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Empresas_CreadaEn");
    }
}
