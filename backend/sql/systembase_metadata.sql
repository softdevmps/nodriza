-- =============================
-- SystemBase - Schema de metadata (sb)
-- Idempotente. Requiere dbo.Roles (ver 01_base_tables.sql).
-- Reconstruido a partir de Comun/BaseDeDatos/SystemBaseContext.cs.
-- Tambien lo embebe SistemasExportador en el database.sql de cada export.
-- =============================

IF SCHEMA_ID(N'sb') IS NULL
    EXEC (N'CREATE SCHEMA sb');
GO

IF OBJECT_ID('sb.Systems', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Systems (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Systems PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(80) NOT NULL,
        Description NVARCHAR(500) NULL,
        Namespace NVARCHAR(200) NULL,
        Version NVARCHAR(20) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_Systems_Status DEFAULT (N'draft'),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_Systems_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_Systems_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        PublishedAt DATETIME2 NULL
    );
    CREATE UNIQUE INDEX UX_sb_Systems_Slug ON sb.Systems (Slug);
END
GO

IF OBJECT_ID('sb.Modules', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Modules (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Modules PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(200) NULL,
        Version NVARCHAR(20) NULL
    );
    CREATE UNIQUE INDEX UX_sb_Modules_Name ON sb.Modules (Name);
END
GO

IF OBJECT_ID('sb.Entities', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Entities (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Entities PRIMARY KEY,
        SystemId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(150) NULL,
        TableName NVARCHAR(128) NOT NULL,
        Description NVARCHAR(500) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_sb_Entities_SortOrder DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_Entities_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_Entities_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_sb_Entities_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id)
    );
    CREATE UNIQUE INDEX UX_sb_Entities_System_Name ON sb.Entities (SystemId, Name);
    CREATE UNIQUE INDEX UX_sb_Entities_System_TableName ON sb.Entities (SystemId, TableName);
END
GO

IF OBJECT_ID('sb.Fields', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Fields (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Fields PRIMARY KEY,
        EntityId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        ColumnName NVARCHAR(128) NOT NULL,
        DataType NVARCHAR(50) NOT NULL,
        Required BIT NOT NULL CONSTRAINT DF_sb_Fields_Required DEFAULT (0),
        MaxLength INT NULL,
        Precision INT NULL,
        Scale INT NULL,
        DefaultValue NVARCHAR(200) NULL,
        IsPrimaryKey BIT NOT NULL CONSTRAINT DF_sb_Fields_IsPrimaryKey DEFAULT (0),
        IsIdentity BIT NOT NULL CONSTRAINT DF_sb_Fields_IsIdentity DEFAULT (0),
        IsUnique BIT NOT NULL CONSTRAINT DF_sb_Fields_IsUnique DEFAULT (0),
        UiConfigJson NVARCHAR(MAX) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_sb_Fields_SortOrder DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_Fields_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_sb_Fields_Entity FOREIGN KEY (EntityId) REFERENCES sb.Entities (Id)
    );
    CREATE UNIQUE INDEX UX_sb_Fields_Entity_Name ON sb.Fields (EntityId, Name);
    CREATE UNIQUE INDEX UX_sb_Fields_Entity_ColumnName ON sb.Fields (EntityId, ColumnName);
END
GO

IF OBJECT_ID('sb.Relations', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Relations (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Relations PRIMARY KEY,
        SystemId INT NOT NULL,
        SourceEntityId INT NOT NULL,
        TargetEntityId INT NOT NULL,
        RelationType NVARCHAR(30) NOT NULL,
        ForeignKey NVARCHAR(128) NULL,
        InverseProperty NVARCHAR(128) NULL,
        CascadeDelete BIT NOT NULL CONSTRAINT DF_sb_Relations_CascadeDelete DEFAULT (0),
        CreatedAt DATETIME2 NULL CONSTRAINT DF_sb_Relations_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_sb_Relations_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id),
        CONSTRAINT FK_sb_Relations_SourceEntity FOREIGN KEY (SourceEntityId) REFERENCES sb.Entities (Id),
        CONSTRAINT FK_sb_Relations_TargetEntity FOREIGN KEY (TargetEntityId) REFERENCES sb.Entities (Id)
    );
END
GO

IF OBJECT_ID('sb.SystemModules', 'U') IS NULL
BEGIN
    CREATE TABLE sb.SystemModules (
        SystemId INT NOT NULL,
        ModuleId INT NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_sb_SystemModules_IsEnabled DEFAULT (1),
        ConfigJson NVARCHAR(MAX) NULL,
        CONSTRAINT PK_sb_SystemModules PRIMARY KEY (SystemId, ModuleId),
        CONSTRAINT FK_sb_SystemModules_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id),
        CONSTRAINT FK_sb_SystemModules_Module FOREIGN KEY (ModuleId) REFERENCES sb.Modules (Id)
    );
END
GO

IF OBJECT_ID('sb.EntityModules', 'U') IS NULL
BEGIN
    CREATE TABLE sb.EntityModules (
        EntityId INT NOT NULL,
        ModuleId INT NOT NULL,
        ConfigJson NVARCHAR(MAX) NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_sb_EntityModules_IsEnabled DEFAULT (1),
        CONSTRAINT PK_sb_EntityModules PRIMARY KEY (EntityId, ModuleId),
        CONSTRAINT FK_sb_EntityModules_Entity FOREIGN KEY (EntityId) REFERENCES sb.Entities (Id),
        CONSTRAINT FK_sb_EntityModules_Module FOREIGN KEY (ModuleId) REFERENCES sb.Modules (Id)
    );
END
GO

IF OBJECT_ID('sb.Permissions', 'U') IS NULL
BEGIN
    CREATE TABLE sb.Permissions (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_Permissions PRIMARY KEY,
        SystemId INT NOT NULL,
        [Key] NVARCHAR(150) NOT NULL,
        Description NVARCHAR(300) NULL,
        CreatedAt DATETIME2 NULL CONSTRAINT DF_sb_Permissions_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_sb_Permissions_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id)
    );
    CREATE UNIQUE INDEX UX_sb_Permissions_System_Key ON sb.Permissions (SystemId, [Key]);
END
GO

IF OBJECT_ID('sb.RolePermissions', 'U') IS NULL
BEGIN
    CREATE TABLE sb.RolePermissions (
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        CONSTRAINT PK_sb_RolePermissions PRIMARY KEY (RoleId, PermissionId),
        CONSTRAINT FK_sb_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id),
        CONSTRAINT FK_sb_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES sb.Permissions (Id)
    );
END
GO

IF OBJECT_ID('sb.SystemMenus', 'U') IS NULL
BEGIN
    CREATE TABLE sb.SystemMenus (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_SystemMenus PRIMARY KEY,
        SystemId INT NOT NULL,
        ParentId INT NULL,
        Title NVARCHAR(100) NOT NULL,
        Route NVARCHAR(200) NULL,
        Icon NVARCHAR(50) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_sb_SystemMenus_SortOrder DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_SystemMenus_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_SystemMenus_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_sb_SystemMenus_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id),
        CONSTRAINT FK_sb_SystemMenus_Parent FOREIGN KEY (ParentId) REFERENCES sb.SystemMenus (Id)
    );
END
GO

IF OBJECT_ID('sb.SystemMenuRoles', 'U') IS NULL
BEGIN
    CREATE TABLE sb.SystemMenuRoles (
        SystemMenuId INT NOT NULL,
        RoleId INT NOT NULL,
        CONSTRAINT PK_sb_SystemMenuRoles PRIMARY KEY (SystemMenuId, RoleId),
        CONSTRAINT FK_sb_SystemMenuRoles_SystemMenu FOREIGN KEY (SystemMenuId) REFERENCES sb.SystemMenus (Id) ON DELETE CASCADE,
        CONSTRAINT FK_sb_SystemMenuRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('sb.SystemBuilds', 'U') IS NULL
BEGIN
    CREATE TABLE sb.SystemBuilds (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sb_SystemBuilds PRIMARY KEY,
        SystemId INT NOT NULL,
        Version NVARCHAR(20) NULL,
        Status NVARCHAR(30) NOT NULL,
        StartedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_SystemBuilds_StartedAt DEFAULT (SYSDATETIME()),
        FinishedAt DATETIME2 NULL,
        Log NVARCHAR(MAX) NULL,
        CONSTRAINT FK_sb_SystemBuilds_System FOREIGN KEY (SystemId) REFERENCES sb.Systems (Id)
    );
END
GO
