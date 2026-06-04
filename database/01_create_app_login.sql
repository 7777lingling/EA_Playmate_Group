USE [master];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.sql_logins
    WHERE name = N'ea_playmate_app'
)
BEGIN
    CREATE LOGIN [ea_playmate_app]
    WITH PASSWORD = N'EaApp@2026!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

USE [EAPlaymateGroup];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = N'ea_playmate_app'
)
BEGIN
    CREATE USER [ea_playmate_app]
    FOR LOGIN [ea_playmate_app];
END
GO

IF IS_ROLEMEMBER(N'db_datareader', N'ea_playmate_app') = 0
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [ea_playmate_app];
END

IF IS_ROLEMEMBER(N'db_datawriter', N'ea_playmate_app') = 0
BEGIN
    ALTER ROLE [db_datawriter] ADD MEMBER [ea_playmate_app];
END
GO
