using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task EnsureAuthColumnsAsync(EAPlaymateGroupDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.login_users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.login_users
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_login_users PRIMARY KEY,
        uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_login_users_uuid DEFAULT NEWID(),
        display_name NVARCHAR(50) NOT NULL,
        login_account NVARCHAR(50) NOT NULL,
        password_hash NVARCHAR(500) NOT NULL,
        system_role NVARCHAR(20) NOT NULL CONSTRAINT DF_login_users_system_role DEFAULT N'staff',
        is_active BIT NOT NULL CONSTRAINT DF_login_users_is_active DEFAULT 1,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_login_users_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL,
        last_login_at DATETIME2 NULL,
        CONSTRAINT CK_login_users_system_role CHECK ([system_role] IN (N'admin', N'staff', N'viewer'))
    );

    CREATE UNIQUE INDEX UQ_login_users_uuid ON dbo.login_users(uuid);
    CREATE UNIQUE INDEX UQ_login_users_login_account ON dbo.login_users(login_account);
END;

IF COL_LENGTH('dbo.users', 'login_account') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD login_account NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.users', 'password_hash') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD password_hash NVARCHAR(500) NULL;
END;

IF COL_LENGTH('dbo.users', 'last_login_at') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD last_login_at DATETIME2 NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_users_login_account'
      AND object_id = OBJECT_ID(N'dbo.users')
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UQ_users_login_account
    ON dbo.users(login_account)
    WHERE login_account IS NOT NULL;');
END;

INSERT INTO dbo.login_users
(
    display_name,
    login_account,
    password_hash,
    system_role,
    is_active,
    created_at,
    updated_at,
    last_login_at
)
SELECT
    u.nickname,
    u.login_account,
    u.password_hash,
    u.system_role,
    u.is_active,
    SYSUTCDATETIME(),
    NULL,
    u.last_login_at
FROM dbo.users u
WHERE u.login_account IS NOT NULL
  AND u.password_hash IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.login_users lu
      WHERE lu.login_account = u.login_account
  );
""");
    }
}
