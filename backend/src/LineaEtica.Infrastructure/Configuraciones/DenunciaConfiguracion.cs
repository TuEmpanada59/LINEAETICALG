using LineaEtica.Domain;
using LineaEtica.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LineaEtica.Infrastructure.Configuraciones;

internal sealed class DenunciaConfiguracion : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> b)
    {
        b.ToTable("Denuncias", t =>
        {
            t.HasCheckConstraint("CK_Denuncias_Modo", "Modo IN (1, 2)");
            t.HasCheckConstraint("CK_Denuncias_Categoria", "Categoria BETWEEN 1 AND 6");
            t.HasCheckConstraint("CK_Denuncias_Estado", "Estado BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_Denuncias_MotivoCierre", "MotivoCierre IS NULL OR MotivoCierre BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_Denuncias_Intentos", "IntentosFallidos >= 0");
            t.HasCheckConstraint("CK_Denuncias_Cierre",
                "(Estado = 4 AND CerradaEn IS NOT NULL AND BorrarDatosEn IS NOT NULL AND MotivoCierre IS NOT NULL) " +
                "OR (Estado <> 4 AND CerradaEn IS NULL AND BorrarDatosEn IS NULL AND MotivoCierre IS NULL)");
            t.HasCheckConstraint("CK_Denuncias_Purga",
                "(DatosPersonalesEliminados = 0 AND RelatoCifrado IS NOT NULL AND HashClave IS NOT NULL) " +
                "OR (DatosPersonalesEliminados = 1 AND Estado = 4 " +
                "AND RelatoCifrado IS NULL AND HashClave IS NULL " +
                "AND FechaHechosCifrada IS NULL AND LugarCifrado IS NULL AND PersonasInvolucradasCifrado IS NULL)");
        });

        b.HasKey(x => x.Id).HasName("PK_Denuncias").IsClustered(false);
        b.HasAlternateKey(x => x.Codigo).HasName("UQ_Denuncias_Codigo");
        b.HasAlternateKey(x => new { x.Id, x.Modo }).HasName("UQ_Denuncias_Id_Modo");

        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()", "DF_Denuncias_Id");
        b.Property(x => x.Codigo).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.HashClave).HasMaxLength(200);
        b.Property(x => x.Estado).HasDefaultValue(EstadoDenuncia.Recibida, "DF_Denuncias_Estado").ValueGeneratedNever();
        b.Property(x => x.CreadaEn).HasDefaultValueSql("SYSUTCDATETIME()", "DF_Denuncias_CreadaEn");
        b.Property(x => x.DatosPersonalesEliminados).HasDefaultValue(false, "DF_Denuncias_Purgada").ValueGeneratedNever();
        b.Property(x => x.IntentosFallidos).HasDefaultValue(0, "DF_Denuncias_Intentos").ValueGeneratedNever();
        b.Property(x => x.FechaHechosCifrada).HasMaxLength(200);
        b.Property(x => x.LugarCifrado).HasMaxLength(1000);
        b.Property(x => x.Version).IsRowVersion();

        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .HasConstraintName("FK_Denuncias_Empresas");
        b.HasOne(x => x.TextoAviso).WithMany().HasForeignKey(x => x.TextoAvisoId)
            .HasConstraintName("FK_Denuncias_TextoAviso");
        b.HasOne(x => x.TextoAutorizacion).WithMany().HasForeignKey(x => x.TextoAutorizacionId)
            .HasConstraintName("FK_Denuncias_TextoAutorizacion");

        b.HasIndex(x => x.CreadaEn).HasDatabaseName("IX_Denuncias_CreadaEn").IsClustered();
        b.HasIndex(x => new { x.EmpresaId, x.Estado }).HasDatabaseName("IX_Denuncias_Empresa_Estado");
        b.HasIndex(x => x.BorrarDatosEn).HasDatabaseName("IX_Denuncias_Purga")
            .HasFilter("[DatosPersonalesEliminados] = 0");
    }
}
