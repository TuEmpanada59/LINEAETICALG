using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class GestorConfiguracion : IEntityTypeConfiguration<Gestor>
{
    public void Configure(EntityTypeBuilder<Gestor> b)
    {
        b.ToTable("Gestores", t =>
        {
            t.HasCheckConstraint("CK_Gestores_Rol", "Rol IN (1, 2)");
            t.HasCheckConstraint("CK_Gestores_Intentos", "IntentosFallidos >= 0");
        });
        b.HasKey(x => x.Id).HasName("PK_Gestores");
        b.HasAlternateKey(x => x.Correo).HasName("UQ_Gestores_Correo");
        b.Property(x => x.Nombre).HasMaxLength(200);
        b.Property(x => x.Correo).HasMaxLength(256);
        b.Property(x => x.HashClave).HasMaxLength(200);
        b.Property(x => x.Activo).HasDefaultValue(true, "DF_Gestores_Activo").ValueGeneratedNever();
        b.Property(x => x.CreadoEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Gestores_CreadoEn");
        b.Property(x => x.TotpSecretoCifrado).HasMaxLength(500);
        b.Property(x => x.DebeCambiarClave).HasDefaultValue(true, "DF_Gestores_DebeCambiarClave").ValueGeneratedNever();
        b.Property(x => x.SelloSesion).HasDefaultValueSql("NEWID()", "DF_Gestores_SelloSesion");
        b.Property(x => x.IntentosFallidos).HasDefaultValue(0, "DF_Gestores_Intentos").ValueGeneratedNever();
    }
}
