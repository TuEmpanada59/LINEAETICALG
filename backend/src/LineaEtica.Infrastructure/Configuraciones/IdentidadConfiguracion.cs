using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class IdentidadConfiguracion : IEntityTypeConfiguration<Identidad>
{
    public void Configure(EntityTypeBuilder<Identidad> b)
    {
        b.ToTable("Identidades", t =>
        {
            t.HasCheckConstraint("CK_Identidades_Modo", "Modo = 1");
            t.HasCheckConstraint("CK_Identidades_Relacion", "Relacion BETWEEN 1 AND 4");
        });
        b.HasKey(x => x.DenunciaId).HasName("PK_Identidades");
        b.Property(x => x.Modo).HasDefaultValue(ModoDenuncia.ANombrePropio, "DF_Identidades_Modo").ValueGeneratedNever();
        b.Property(x => x.NombreCifrado).HasMaxLength(1000);
        b.Property(x => x.CorreoCifrado).HasMaxLength(1000);
        b.Property(x => x.TelefonoCifrado).HasMaxLength(500);

        // Llave compuesta: impide a nivel de base que una denuncia anónima tenga identidad.
        b.HasOne(x => x.Denuncia).WithOne(x => x.Identidad)
            .HasPrincipalKey<Denuncia>(x => new { x.Id, x.Modo })
            .HasForeignKey<Identidad>(x => new { x.DenunciaId, x.Modo })
            .HasConstraintName("FK_Identidades_Denuncias");
    }
}
