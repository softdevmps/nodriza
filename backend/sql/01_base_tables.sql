-- =============================
-- SystemBase - Tablas base (dbo)
-- Idempotente. Ejecutar sobre la base de SystemBase (sqlcmd -d <DB_NAME>).
-- Debe ir antes de systembase_metadata.sql (sb referencia dbo.Roles).
-- =============================

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Nombre VARCHAR(50) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1)
    );
END
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        Username VARCHAR(50) NOT NULL,
        Email VARCHAR(100) NOT NULL,
        PasswordHash VARCHAR(255) NOT NULL,
        Nombre VARCHAR(100) NOT NULL,
        Apellido VARCHAR(100) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (GETDATE()),
        RolId INT NULL,
        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles (Id)
    );
    CREATE UNIQUE INDEX UX_Usuarios_Username ON dbo.Usuarios (Username);
    CREATE UNIQUE INDEX UX_Usuarios_Email ON dbo.Usuarios (Email);
END
GO

IF OBJECT_ID('dbo.Menus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Menus (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Menus PRIMARY KEY,
        Titulo VARCHAR(100) NOT NULL,
        Icono VARCHAR(50) NOT NULL,
        Ruta VARCHAR(100) NULL,
        Orden INT NOT NULL,
        PadreId INT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Menus_Activo DEFAULT (1),
        CONSTRAINT FK_Menus_Padre FOREIGN KEY (PadreId) REFERENCES dbo.Menus (Id)
    );
    CREATE INDEX IX_Menus_Activo ON dbo.Menus (Activo);
    CREATE INDEX IX_Menus_Orden ON dbo.Menus (Orden);
    CREATE INDEX IX_Menus_PadreId ON dbo.Menus (PadreId);
END
GO

IF OBJECT_ID('dbo.RolMenu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolMenu (
        RolId INT NOT NULL,
        MenuId INT NOT NULL,
        CONSTRAINT PK_RolMenu PRIMARY KEY (RolId, MenuId),
        CONSTRAINT FK_RolMenu_Rol FOREIGN KEY (RolId) REFERENCES dbo.Roles (Id),
        CONSTRAINT FK_RolMenu_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menus (Id)
    );
END
GO
