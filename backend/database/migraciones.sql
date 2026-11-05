IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Empresas] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [Activa] bit NOT NULL CONSTRAINT [DF_Empresas_Activa] DEFAULT CAST(1 AS bit),
        [CreadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Empresas_CreadaEn] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Empresas] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Empresas_Nombre] UNIQUE ([Nombre])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Gestores] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [Correo] nvarchar(256) NOT NULL,
        [HashClave] nvarchar(200) NOT NULL,
        [Rol] tinyint NOT NULL,
        [Activo] bit NOT NULL CONSTRAINT [DF_Gestores_Activo] DEFAULT CAST(1 AS bit),
        [CreadoEn] datetime2(0) NOT NULL CONSTRAINT [DF_Gestores_CreadoEn] DEFAULT (SYSUTCDATETIME()),
        [TotpSecretoCifrado] varbinary(500) NULL,
        [TotpConfirmadoEn] datetime2(0) NULL,
        [TotpUltimoPaso] bigint NULL,
        [DebeCambiarClave] bit NOT NULL CONSTRAINT [DF_Gestores_DebeCambiarClave] DEFAULT CAST(1 AS bit),
        [SelloSesion] uniqueidentifier NOT NULL CONSTRAINT [DF_Gestores_SelloSesion] DEFAULT (NEWID()),
        [IntentosFallidos] int NOT NULL CONSTRAINT [DF_Gestores_Intentos] DEFAULT 0,
        [BloqueadoHasta] datetime2(0) NULL,
        CONSTRAINT [PK_Gestores] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Gestores_Correo] UNIQUE ([Correo]),
        CONSTRAINT [CK_Gestores_Intentos] CHECK (IntentosFallidos >= 0),
        CONSTRAINT [CK_Gestores_Rol] CHECK (Rol IN (1, 2))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Configuracion] (
        [Clave] varchar(100) NOT NULL,
        [Valor] nvarchar(max) NOT NULL,
        [Descripcion] nvarchar(300) NULL,
        [ModificadaPorId] int NULL,
        [ModificadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Configuracion_En] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Configuracion] PRIMARY KEY ([Clave]),
        CONSTRAINT [FK_Configuracion_Gestores] FOREIGN KEY ([ModificadaPorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [TextosLegales] (
        [Id] int NOT NULL IDENTITY,
        [Tipo] tinyint NOT NULL,
        [Version] int NOT NULL,
        [Contenido] nvarchar(max) NOT NULL,
        [VigenteDesde] datetime2(0) NOT NULL CONSTRAINT [DF_TextosLegales_Vigente] DEFAULT (SYSUTCDATETIME()),
        [PublicadoPorId] int NULL,
        CONSTRAINT [PK_TextosLegales] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_TextosLegales_Tipo_Version] UNIQUE ([Tipo], [Version]),
        CONSTRAINT [CK_TextosLegales_Tipo] CHECK (Tipo IN (1, 2, 3)),
        CONSTRAINT [FK_TextosLegales_Gestores] FOREIGN KEY ([PublicadoPorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Denuncias] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Denuncias_Id] DEFAULT (NEWSEQUENTIALID()),
        [Codigo] varchar(20) NOT NULL,
        [HashClave] nvarchar(200) NULL,
        [EmpresaId] int NOT NULL,
        [Modo] tinyint NOT NULL,
        [Categoria] tinyint NOT NULL,
        [RelatoCifrado] varbinary(max) NULL,
        [Estado] tinyint NOT NULL CONSTRAINT [DF_Denuncias_Estado] DEFAULT CAST(1 AS tinyint),
        [CreadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Denuncias_CreadaEn] DEFAULT (SYSUTCDATETIME()),
        [VenceRespuestaEn] datetime2(0) NULL,
        [CerradaEn] datetime2(0) NULL,
        [BorrarDatosEn] datetime2(0) NULL,
        [DatosPersonalesEliminados] bit NOT NULL CONSTRAINT [DF_Denuncias_Purgada] DEFAULT CAST(0 AS bit),
        [AceptoTratamientoEn] datetime2(0) NOT NULL,
        [TextoAvisoId] int NOT NULL,
        [TextoAutorizacionId] int NOT NULL,
        [MotivoCierre] tinyint NULL,
        [IntentosFallidos] int NOT NULL CONSTRAINT [DF_Denuncias_Intentos] DEFAULT 0,
        [BloqueadaHasta] datetime2(0) NULL,
        [FechaHechosCifrada] varbinary(200) NULL,
        [LugarCifrado] varbinary(1000) NULL,
        [PersonasInvolucradasCifrado] varbinary(max) NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_Denuncias] PRIMARY KEY NONCLUSTERED ([Id]),
        CONSTRAINT [UQ_Denuncias_Codigo] UNIQUE ([Codigo]),
        CONSTRAINT [UQ_Denuncias_Id_Modo] UNIQUE ([Id], [Modo]),
        CONSTRAINT [CK_Denuncias_Categoria] CHECK (Categoria BETWEEN 1 AND 6),
        CONSTRAINT [CK_Denuncias_Cierre] CHECK ((Estado = 4 AND CerradaEn IS NOT NULL AND BorrarDatosEn IS NOT NULL AND MotivoCierre IS NOT NULL) OR (Estado <> 4 AND CerradaEn IS NULL AND BorrarDatosEn IS NULL AND MotivoCierre IS NULL)),
        CONSTRAINT [CK_Denuncias_Estado] CHECK (Estado BETWEEN 1 AND 4),
        CONSTRAINT [CK_Denuncias_Intentos] CHECK (IntentosFallidos >= 0),
        CONSTRAINT [CK_Denuncias_Modo] CHECK (Modo IN (1, 2)),
        CONSTRAINT [CK_Denuncias_MotivoCierre] CHECK (MotivoCierre IS NULL OR MotivoCierre BETWEEN 1 AND 4),
        CONSTRAINT [CK_Denuncias_Purga] CHECK ((DatosPersonalesEliminados = 0 AND RelatoCifrado IS NOT NULL AND HashClave IS NOT NULL) OR (DatosPersonalesEliminados = 1 AND Estado = 4 AND RelatoCifrado IS NULL AND HashClave IS NULL AND FechaHechosCifrada IS NULL AND LugarCifrado IS NULL AND PersonasInvolucradasCifrado IS NULL)),
        CONSTRAINT [FK_Denuncias_Empresas] FOREIGN KEY ([EmpresaId]) REFERENCES [Empresas] ([Id]),
        CONSTRAINT [FK_Denuncias_TextoAutorizacion] FOREIGN KEY ([TextoAutorizacionId]) REFERENCES [TextosLegales] ([Id]),
        CONSTRAINT [FK_Denuncias_TextoAviso] FOREIGN KEY ([TextoAvisoId]) REFERENCES [TextosLegales] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Actuaciones] (
        [Id] bigint NOT NULL IDENTITY,
        [DenunciaId] uniqueidentifier NULL,
        [ActorTipo] tinyint NOT NULL,
        [GestorId] int NULL,
        [Accion] nvarchar(100) NOT NULL,
        [Detalle] nvarchar(1000) NULL,
        [CreadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Actuaciones_CreadaEn] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Actuaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Actuaciones_Actor] CHECK ((ActorTipo = 1 AND GestorId IS NULL AND DenunciaId IS NOT NULL) OR (ActorTipo = 2 AND GestorId IS NOT NULL) OR (ActorTipo = 3 AND GestorId IS NULL)),
        CONSTRAINT [FK_Actuaciones_Denuncias] FOREIGN KEY ([DenunciaId]) REFERENCES [Denuncias] ([Id]),
        CONSTRAINT [FK_Actuaciones_Gestores] FOREIGN KEY ([GestorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Anotaciones] (
        [Id] bigint NOT NULL IDENTITY,
        [DenunciaId] uniqueidentifier NOT NULL,
        [GestorId] int NOT NULL,
        [TextoCifrado] varbinary(max) NOT NULL,
        [CreadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Anotaciones_CreadaEn] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Anotaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Anotaciones_Denuncias] FOREIGN KEY ([DenunciaId]) REFERENCES [Denuncias] ([Id]),
        CONSTRAINT [FK_Anotaciones_Gestores] FOREIGN KEY ([GestorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Asignaciones] (
        [DenunciaId] uniqueidentifier NOT NULL,
        [GestorId] int NOT NULL,
        [AsignadaPorId] int NOT NULL,
        [AsignadaEn] datetime2(0) NOT NULL CONSTRAINT [DF_Asignaciones_En] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Asignaciones] PRIMARY KEY ([DenunciaId], [GestorId]),
        CONSTRAINT [FK_Asignaciones_AsignadaPor] FOREIGN KEY ([AsignadaPorId]) REFERENCES [Gestores] ([Id]),
        CONSTRAINT [FK_Asignaciones_Denuncias] FOREIGN KEY ([DenunciaId]) REFERENCES [Denuncias] ([Id]),
        CONSTRAINT [FK_Asignaciones_Gestores] FOREIGN KEY ([GestorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Identidades] (
        [DenunciaId] uniqueidentifier NOT NULL,
        [Modo] tinyint NOT NULL CONSTRAINT [DF_Identidades_Modo] DEFAULT CAST(1 AS tinyint),
        [NombreCifrado] varbinary(1000) NOT NULL,
        [CorreoCifrado] varbinary(1000) NOT NULL,
        [TelefonoCifrado] varbinary(500) NULL,
        [Relacion] tinyint NOT NULL,
        CONSTRAINT [PK_Identidades] PRIMARY KEY ([DenunciaId]),
        CONSTRAINT [CK_Identidades_Modo] CHECK (Modo = 1),
        CONSTRAINT [CK_Identidades_Relacion] CHECK (Relacion BETWEEN 1 AND 4),
        CONSTRAINT [FK_Identidades_Denuncias] FOREIGN KEY ([DenunciaId], [Modo]) REFERENCES [Denuncias] ([Id], [Modo])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE TABLE [Mensajes] (
        [Id] bigint NOT NULL IDENTITY,
        [DenunciaId] uniqueidentifier NOT NULL,
        [Autor] tinyint NOT NULL,
        [GestorId] int NULL,
        [TextoCifrado] varbinary(max) NOT NULL,
        [CreadoEn] datetime2(0) NOT NULL CONSTRAINT [DF_Mensajes_CreadoEn] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Mensajes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Mensajes_Autor] CHECK ((Autor = 1 AND GestorId IS NULL) OR (Autor = 2 AND GestorId IS NOT NULL)),
        CONSTRAINT [FK_Mensajes_Denuncias] FOREIGN KEY ([DenunciaId]) REFERENCES [Denuncias] ([Id]),
        CONSTRAINT [FK_Mensajes_Gestores] FOREIGN KEY ([GestorId]) REFERENCES [Gestores] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE INDEX [IX_Actuaciones_Denuncia] ON [Actuaciones] ([DenunciaId], [CreadaEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE INDEX [IX_Anotaciones_Denuncia] ON [Anotaciones] ([DenunciaId], [CreadaEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE INDEX [IX_Asignaciones_Gestor] ON [Asignaciones] ([GestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE CLUSTERED INDEX [IX_Denuncias_CreadaEn] ON [Denuncias] ([CreadaEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE INDEX [IX_Denuncias_Empresa_Estado] ON [Denuncias] ([EmpresaId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Denuncias_Purga] ON [Denuncias] ([BorrarDatosEn]) WHERE [DatosPersonalesEliminados] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE INDEX [IX_Mensajes_Denuncia] ON [Mensajes] ([DenunciaId], [CreadoEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    EXEC (N'CREATE TRIGGER dbo.TR_Actuaciones_SoloInsertar
    ON dbo.Actuaciones
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        THROW 50001, N''La bitácora de actuaciones no se puede modificar ni borrar.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    EXEC (N'CREATE TRIGGER dbo.TR_TextosLegales_SoloInsertar
    ON dbo.TextosLegales
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        THROW 50002, N''Un texto legal publicado no se modifica: se publica una versión nueva.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    CREATE ROLE rol_lineaetica_api;
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO rol_lineaetica_api;
    DENY UPDATE, DELETE ON dbo.Actuaciones   TO rol_lineaetica_api;
    DENY UPDATE, DELETE ON dbo.TextosLegales TO rol_lineaetica_api;
    DENY DELETE ON dbo.Denuncias TO rol_lineaetica_api;
    DENY DELETE ON dbo.Gestores  TO rol_lineaetica_api;
    DENY DELETE ON dbo.Empresas  TO rol_lineaetica_api;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    INSERT INTO dbo.Empresas (Nombre) VALUES (N'Londoño Gómez');

    INSERT INTO dbo.Configuracion (Clave, Valor, Descripcion) VALUES
     ('PlazoRespuestaDias',      N'15',  N'Días para responder al denunciante. PENDIENTE de definir.'),
     ('RetencionDiasTrasCierre', N'365', N'Días que se conservan los datos personales después del cierre. PENDIENTE de definir.'),
     ('TextoAcuse',              N'Hemos recibido su denuncia. Guarde su código y contraseña para consultar el estado del caso y comunicarse con nosotros.', N'Mensaje de acuse de recibo. PENDIENTE de redacción jurídica.');

    INSERT INTO dbo.TextosLegales (Tipo, Version, Contenido) VALUES
     (1, 1, N'(Borrador pendiente de redacción jurídica)'),
     (2, 1, N'Autorizo el tratamiento de mis datos personales para la gestión de esta denuncia, conforme al aviso de privacidad.'),
     (3, 1, N'(Borrador pendiente de redacción jurídica)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105172243_EstructuraInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105172243_EstructuraInicial', N'10.0.14');
END;

COMMIT;
GO

