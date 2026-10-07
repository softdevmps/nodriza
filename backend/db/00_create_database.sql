-- =============================
-- SystemBase - Crear base de datos
-- Uso: sqlcmd ... -v DB_NAME=systemBase -i 00_create_database.sql
-- =============================

IF DB_ID(N'$(DB_NAME)') IS NULL
BEGIN
    CREATE DATABASE [$(DB_NAME)];
END
GO
