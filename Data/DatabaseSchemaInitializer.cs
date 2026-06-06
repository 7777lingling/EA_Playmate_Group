using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task EnsureAuthColumnsAsync(EAPlaymateGroupDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
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
""");
    }
}
