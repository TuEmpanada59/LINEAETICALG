using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LineaEtica.Infrastructure.Migraciones
{
    /// <inheritdoc />
    public partial class EstructuraInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Empresas_Activa"),
                    CreadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Empresas_CreadaEn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.UniqueConstraint("UQ_Empresas_Nombre", x => x.Nombre);
                });

            migrationBuilder.CreateTable(
                name: "Gestores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HashClave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rol = table.Column<byte>(type: "tinyint", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Gestores_Activo"),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Gestores_CreadoEn"),
                    TotpSecretoCifrado = table.Column<byte[]>(type: "varbinary(500)", maxLength: 500, nullable: true),
                    TotpConfirmadoEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    TotpUltimoPaso = table.Column<long>(type: "bigint", nullable: true),
                    DebeCambiarClave = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Gestores_DebeCambiarClave"),
                    SelloSesion = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Gestores_SelloSesion"),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_Gestores_Intentos"),
                    BloqueadoHasta = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gestores", x => x.Id);
                    table.UniqueConstraint("UQ_Gestores_Correo", x => x.Correo);
                    table.CheckConstraint("CK_Gestores_Intentos", "IntentosFallidos >= 0");
                    table.CheckConstraint("CK_Gestores_Rol", "Rol IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "Configuracion",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ModificadaPorId = table.Column<int>(type: "int", nullable: true),
                    ModificadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Configuracion_En")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion", x => x.Clave);
                    table.ForeignKey(
                        name: "FK_Configuracion_Gestores",
                        column: x => x.ModificadaPorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TextosLegales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<byte>(type: "tinyint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_TextosLegales_Vigente"),
                    PublicadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextosLegales", x => x.Id);
                    table.UniqueConstraint("UQ_TextosLegales_Tipo_Version", x => new { x.Tipo, x.Version });
                    table.CheckConstraint("CK_TextosLegales_Tipo", "Tipo IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_TextosLegales_Gestores",
                        column: x => x.PublicadoPorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Denuncias_Id"),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    HashClave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Modo = table.Column<byte>(type: "tinyint", nullable: false),
                    Categoria = table.Column<byte>(type: "tinyint", nullable: false),
                    RelatoCifrado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Denuncias_Estado"),
                    CreadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Denuncias_CreadaEn"),
                    VenceRespuestaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CerradaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    BorrarDatosEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    DatosPersonalesEliminados = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_Denuncias_Purgada"),
                    AceptoTratamientoEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    TextoAvisoId = table.Column<int>(type: "int", nullable: false),
                    TextoAutorizacionId = table.Column<int>(type: "int", nullable: false),
                    MotivoCierre = table.Column<byte>(type: "tinyint", nullable: true),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_Denuncias_Intentos"),
                    BloqueadaHasta = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    FechaHechosCifrada = table.Column<byte[]>(type: "varbinary(200)", maxLength: 200, nullable: true),
                    LugarCifrado = table.Column<byte[]>(type: "varbinary(1000)", maxLength: 1000, nullable: true),
                    PersonasInvolucradasCifrado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.UniqueConstraint("UQ_Denuncias_Codigo", x => x.Codigo);
                    table.UniqueConstraint("UQ_Denuncias_Id_Modo", x => new { x.Id, x.Modo });
                    table.CheckConstraint("CK_Denuncias_Categoria", "Categoria BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_Denuncias_Cierre", "(Estado = 4 AND CerradaEn IS NOT NULL AND BorrarDatosEn IS NOT NULL AND MotivoCierre IS NOT NULL) OR (Estado <> 4 AND CerradaEn IS NULL AND BorrarDatosEn IS NULL AND MotivoCierre IS NULL)");
                    table.CheckConstraint("CK_Denuncias_Estado", "Estado BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_Denuncias_Intentos", "IntentosFallidos >= 0");
                    table.CheckConstraint("CK_Denuncias_Modo", "Modo IN (1, 2)");
                    table.CheckConstraint("CK_Denuncias_MotivoCierre", "MotivoCierre IS NULL OR MotivoCierre BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_Denuncias_Purga", "(DatosPersonalesEliminados = 0 AND RelatoCifrado IS NOT NULL AND HashClave IS NOT NULL) OR (DatosPersonalesEliminados = 1 AND Estado = 4 AND RelatoCifrado IS NULL AND HashClave IS NULL AND FechaHechosCifrada IS NULL AND LugarCifrado IS NULL AND PersonasInvolucradasCifrado IS NULL)");
                    table.ForeignKey(
                        name: "FK_Denuncias_Empresas",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Denuncias_TextoAutorizacion",
                        column: x => x.TextoAutorizacionId,
                        principalTable: "TextosLegales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Denuncias_TextoAviso",
                        column: x => x.TextoAvisoId,
                        principalTable: "TextosLegales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Actuaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorTipo = table.Column<byte>(type: "tinyint", nullable: false),
                    GestorId = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Actuaciones_CreadaEn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actuaciones", x => x.Id);
                    table.CheckConstraint("CK_Actuaciones_Actor", "(ActorTipo = 1 AND GestorId IS NULL AND DenunciaId IS NOT NULL) OR (ActorTipo = 2 AND GestorId IS NOT NULL) OR (ActorTipo = 3 AND GestorId IS NULL)");
                    table.ForeignKey(
                        name: "FK_Actuaciones_Denuncias",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Actuaciones_Gestores",
                        column: x => x.GestorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Anotaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GestorId = table.Column<int>(type: "int", nullable: false),
                    TextoCifrado = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Anotaciones_CreadaEn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anotaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anotaciones_Denuncias",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Anotaciones_Gestores",
                        column: x => x.GestorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GestorId = table.Column<int>(type: "int", nullable: false),
                    AsignadaPorId = table.Column<int>(type: "int", nullable: false),
                    AsignadaEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Asignaciones_En")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => new { x.DenunciaId, x.GestorId });
                    table.ForeignKey(
                        name: "FK_Asignaciones_AsignadaPor",
                        column: x => x.AsignadaPorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asignaciones_Denuncias",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asignaciones_Gestores",
                        column: x => x.GestorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Identidades",
                columns: table => new
                {
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modo = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Identidades_Modo"),
                    NombreCifrado = table.Column<byte[]>(type: "varbinary(1000)", maxLength: 1000, nullable: false),
                    CorreoCifrado = table.Column<byte[]>(type: "varbinary(1000)", maxLength: 1000, nullable: false),
                    TelefonoCifrado = table.Column<byte[]>(type: "varbinary(500)", maxLength: 500, nullable: true),
                    Relacion = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identidades", x => x.DenunciaId);
                    table.CheckConstraint("CK_Identidades_Modo", "Modo = 1");
                    table.CheckConstraint("CK_Identidades_Relacion", "Relacion BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_Identidades_Denuncias",
                        columns: x => new { x.DenunciaId, x.Modo },
                        principalTable: "Denuncias",
                        principalColumns: new[] { "Id", "Modo" });
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Autor = table.Column<byte>(type: "tinyint", nullable: false),
                    GestorId = table.Column<int>(type: "int", nullable: true),
                    TextoCifrado = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Mensajes_CreadoEn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                    table.CheckConstraint("CK_Mensajes_Autor", "(Autor = 1 AND GestorId IS NULL) OR (Autor = 2 AND GestorId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Mensajes_Denuncias",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mensajes_Gestores",
                        column: x => x.GestorId,
                        principalTable: "Gestores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actuaciones_Denuncia",
                table: "Actuaciones",
                columns: new[] { "DenunciaId", "CreadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Anotaciones_Denuncia",
                table: "Anotaciones",
                columns: new[] { "DenunciaId", "CreadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_Gestor",
                table: "Asignaciones",
                column: "GestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_CreadaEn",
                table: "Denuncias",
                column: "CreadaEn")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Empresa_Estado",
                table: "Denuncias",
                columns: new[] { "EmpresaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Purga",
                table: "Denuncias",
                column: "BorrarDatosEn",
                filter: "[DatosPersonalesEliminados] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_Denuncia",
                table: "Mensajes",
                columns: new[] { "DenunciaId", "CreadoEn" });

            // Con EXEC porque el script idempotente envuelve cada paso en un IF, y CREATE TRIGGER debe ir solo en su lote.
            migrationBuilder.Sql("""
                EXEC (N'CREATE TRIGGER dbo.TR_Actuaciones_SoloInsertar
                ON dbo.Actuaciones
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    THROW 50001, N''La bitácora de actuaciones no se puede modificar ni borrar.'', 1;
                END;');
                """);

            migrationBuilder.Sql("""
                EXEC (N'CREATE TRIGGER dbo.TR_TextosLegales_SoloInsertar
                ON dbo.TextosLegales
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    THROW 50002, N''Un texto legal publicado no se modifica: se publica una versión nueva.'', 1;
                END;');
                """);

            migrationBuilder.Sql("""
                CREATE ROLE rol_lineaetica_api;
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO rol_lineaetica_api;
                DENY UPDATE, DELETE ON dbo.Actuaciones   TO rol_lineaetica_api;
                DENY UPDATE, DELETE ON dbo.TextosLegales TO rol_lineaetica_api;
                DENY DELETE ON dbo.Denuncias TO rol_lineaetica_api;
                DENY DELETE ON dbo.Gestores  TO rol_lineaetica_api;
                DENY DELETE ON dbo.Empresas  TO rol_lineaetica_api;
                """);

            // Valores provisionales: plazos y textos pendientes de la asesoría jurídica.
            migrationBuilder.Sql("""
                INSERT INTO dbo.Empresas (Nombre) VALUES (N'Londoño Gómez');

                INSERT INTO dbo.Configuracion (Clave, Valor, Descripcion) VALUES
                 ('PlazoRespuestaDias',      N'15',  N'Días para responder al denunciante. PENDIENTE de definir.'),
                 ('RetencionDiasTrasCierre', N'365', N'Días que se conservan los datos personales después del cierre. PENDIENTE de definir.'),
                 ('TextoAcuse',              N'Hemos recibido su denuncia. Guarde su código y contraseña para consultar el estado del caso y comunicarse con nosotros.', N'Mensaje de acuse de recibo. PENDIENTE de redacción jurídica.');

                INSERT INTO dbo.TextosLegales (Tipo, Version, Contenido) VALUES
                 (1, 1, N'(Borrador pendiente de redacción jurídica)'),
                 (2, 1, N'Autorizo el tratamiento de mis datos personales para la gestión de esta denuncia, conforme al aviso de privacidad.'),
                 (3, 1, N'(Borrador pendiente de redacción jurídica)');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP ROLE rol_lineaetica_api;");

            migrationBuilder.DropTable(
                name: "Actuaciones");

            migrationBuilder.DropTable(
                name: "Anotaciones");

            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "Configuracion");

            migrationBuilder.DropTable(
                name: "Identidades");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "Denuncias");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "TextosLegales");

            migrationBuilder.DropTable(
                name: "Gestores");
        }
    }
}
