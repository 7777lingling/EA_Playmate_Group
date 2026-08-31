using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task EnsureOrderColumnsAsync(EAPlaymateGroupDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.orders', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.activities', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.activities
        (
            id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_activities PRIMARY KEY,
            organization_id INT NOT NULL,
            uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_activities_uuid DEFAULT NEWID(),
            name NVARCHAR(100) NOT NULL,
            starts_at DATETIME2 NOT NULL,
            ends_at DATETIME2 NOT NULL,
            discount_type NVARCHAR(30) NOT NULL,
            discount_value DECIMAL(10,2) NOT NULL,
            applicable_categories NVARCHAR(500) NOT NULL CONSTRAINT DF_activities_applicable_categories DEFAULT N'',
            include_fees BIT NOT NULL CONSTRAINT DF_activities_include_fees DEFAULT 0,
            is_active BIT NOT NULL CONSTRAINT DF_activities_is_active DEFAULT 1,
            note NVARCHAR(500) NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_activities_created_at DEFAULT SYSUTCDATETIME(),
            updated_at DATETIME2 NULL,
            CONSTRAINT UQ_activities_uuid UNIQUE (uuid),
            CONSTRAINT CK_activities_period CHECK (ends_at >= starts_at),
            CONSTRAINT CK_activities_discount_type CHECK (discount_type IN (N'percent', N'fixed_amount', N'fixed_price')),
            CONSTRAINT CK_activities_discount_value CHECK (discount_value >= 0 AND (discount_type <> N'percent' OR discount_value <= 100))
        );
        CREATE INDEX IX_activities_scope ON dbo.activities(organization_id, is_active, starts_at, ends_at);
    END;

    IF COL_LENGTH('dbo.orders', 'order_type') IS NULL
        ALTER TABLE dbo.orders ADD order_type NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_order_type DEFAULT N'boosting';
    IF COL_LENGTH('dbo.orders', 'pricing_category') IS NULL
        ALTER TABLE dbo.orders ADD pricing_category NVARCHAR(50) NULL;

    IF COL_LENGTH('dbo.orders', 'service_quantity') IS NULL
        ALTER TABLE dbo.orders ADD service_quantity DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_service_quantity DEFAULT 0;

    IF COL_LENGTH('dbo.orders', 'base_amount') IS NULL
        ALTER TABLE dbo.orders ADD base_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_base_amount DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'designated_fee') IS NULL
        ALTER TABLE dbo.orders ADD designated_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_designated_fee DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'friend_fee') IS NULL
        ALTER TABLE dbo.orders ADD friend_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_friend_fee DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'replacement_fee') IS NULL
        ALTER TABLE dbo.orders ADD replacement_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_replacement_fee DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'night_fee') IS NULL
        ALTER TABLE dbo.orders ADD night_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_night_fee DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'other_fee') IS NULL
        ALTER TABLE dbo.orders ADD other_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_other_fee DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'discount_amount') IS NULL
        ALTER TABLE dbo.orders ADD discount_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_discount_amount DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'final_amount') IS NULL
        ALTER TABLE dbo.orders ADD final_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_final_amount DEFAULT 0;
    IF COL_LENGTH('dbo.orders', 'activity_id') IS NULL
        ALTER TABLE dbo.orders ADD activity_id INT NULL;
    IF COL_LENGTH('dbo.orders', 'activity_name_snapshot') IS NULL
        ALTER TABLE dbo.orders ADD activity_name_snapshot NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.orders', 'activity_discount_type') IS NULL
        ALTER TABLE dbo.orders ADD activity_discount_type NVARCHAR(30) NULL;
    IF COL_LENGTH('dbo.orders', 'activity_discount_value') IS NULL
        ALTER TABLE dbo.orders ADD activity_discount_value DECIMAL(10,2) NULL;
    IF COL_LENGTH('dbo.orders', 'activity_include_fees') IS NULL
        ALTER TABLE dbo.orders ADD activity_include_fees BIT NOT NULL CONSTRAINT DF_orders_activity_include_fees DEFAULT 0;

    EXEC(N'UPDATE dbo.orders
    SET base_amount = amount,
        final_amount = amount
    WHERE amount > 0
      AND base_amount = 0
      AND designated_fee = 0
      AND friend_fee = 0
      AND replacement_fee = 0
      AND night_fee = 0
      AND other_fee = 0
      AND discount_amount = 0
      AND final_amount = 0;');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_orders_pricing_non_negative'
          AND parent_object_id = OBJECT_ID(N'dbo.orders')
    )
        EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_pricing_non_negative CHECK (base_amount >= 0 AND designated_fee >= 0 AND friend_fee >= 0 AND replacement_fee >= 0 AND night_fee >= 0 AND other_fee >= 0 AND discount_amount >= 0 AND final_amount >= 0)');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_orders_discount_amount'
          AND parent_object_id = OBJECT_ID(N'dbo.orders')
    )
        EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_discount_amount CHECK (discount_amount <= base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee)');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_orders_final_amount'
          AND parent_object_id = OBJECT_ID(N'dbo.orders')
    )
        EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_final_amount CHECK (final_amount = base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee - discount_amount)');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_orders_activity'
    )
        EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT FK_orders_activity FOREIGN KEY (activity_id) REFERENCES dbo.activities(id)');

    IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
       AND NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_activities_organization'
    )
        EXEC(N'ALTER TABLE dbo.activities WITH CHECK ADD CONSTRAINT FK_activities_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id)');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_orders_activity_id'
          AND object_id = OBJECT_ID(N'dbo.orders')
    )
        CREATE INDEX IX_orders_activity_id ON dbo.orders(activity_id);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_orders_order_type'
          AND parent_object_id = OBJECT_ID(N'dbo.orders')
    )
        EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_order_type CHECK (order_type IN (N''boosting'', N''farming'', N''companion'', N''prepaid''))');
END;
""");
    }

    public static async Task ValidateOrganizationFiltersAsync(EAPlaymateGroupDbContext db)
    {
        await db.LoginUsers.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.UserPreferences.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Users.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Orders.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.OrderMembers.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Payments.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.AuditLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.MoneyLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.ServiceItems.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Activities.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.GiftRecords.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Departments.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.DepartmentMembers.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
    }

    public static async Task EnsureAuthColumnsAsync(EAPlaymateGroupDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.role_permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.role_permissions
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_role_permissions PRIMARY KEY,
        system_role NVARCHAR(20) NOT NULL,
        permission_code NVARCHAR(80) NOT NULL,
        is_allowed BIT NOT NULL CONSTRAINT DF_role_permissions_is_allowed DEFAULT 0,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_role_permissions_updated_at DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UQ_role_permissions_role_code
    ON dbo.role_permissions(system_role, permission_code);
END;

WITH defaults AS
(
    SELECT *
    FROM (VALUES
        (N'staff', N'Member.View', 1),
        (N'staff', N'Member.Create', 1),
        (N'staff', N'Member.Edit', 1),
        (N'staff', N'Member.Delete', 1),
        (N'staff', N'Gift.View', 1),
        (N'staff', N'Gift.Create', 1),
        (N'staff', N'Gift.Edit', 1),
        (N'staff', N'Gift.Delete', 1),
        (N'staff', N'Order.View', 1),
        (N'staff', N'Order.Create', 1),
        (N'staff', N'Order.Edit', 1),
        (N'staff', N'Order.Cancel', 1),
        (N'staff', N'Settlement.View', 1),
        (N'staff', N'Account.Manage', 1),
        (N'staff', N'Organization.Manage', 1),
        (N'staff', N'Audit.View', 1),
        (N'staff', N'Profile.Manage', 1),
        (N'viewer', N'Member.View', 1),
        (N'viewer', N'Gift.View', 1),
        (N'viewer', N'Order.View', 1),
        (N'viewer', N'Profile.Manage', 1)
    ) AS value(system_role, permission_code, is_allowed)
)
INSERT INTO dbo.role_permissions(system_role, permission_code, is_allowed, updated_at)
SELECT d.system_role, d.permission_code, d.is_allowed, SYSUTCDATETIME()
FROM defaults d
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.role_permissions existing
    WHERE existing.system_role = d.system_role
      AND existing.permission_code = d.permission_code
);
""");

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

IF COL_LENGTH('dbo.login_users', 'discord_id') IS NULL
BEGIN
    ALTER TABLE dbo.login_users ADD discord_id NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.login_users', 'discord_name') IS NULL
BEGIN
    ALTER TABLE dbo.login_users ADD discord_name NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.users', 'discord_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD discord_user_id NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.login_users', 'discord_linked_at') IS NULL
BEGIN
    ALTER TABLE dbo.login_users ADD discord_linked_at DATETIME2 NULL;
    EXEC(N'UPDATE dbo.login_users
    SET discord_linked_at = SYSUTCDATETIME()
    WHERE discord_id IS NOT NULL
      AND discord_id NOT LIKE ''%[^0-9]%''
      AND LEN(discord_id) BETWEEN 17 AND 20;');
END;

IF COL_LENGTH('dbo.login_users', 'discord_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.login_users ADD discord_user_id NVARCHAR(50) NULL;
    EXEC(N'UPDATE dbo.login_users
    SET discord_user_id = discord_id,
        discord_id = discord_name,
        discord_name = NULL
    WHERE discord_linked_at IS NOT NULL
      AND discord_id IS NOT NULL
      AND discord_id NOT LIKE ''%[^0-9]%''
      AND LEN(discord_id) BETWEEN 17 AND 20;');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_login_users_discord_id'
      AND object_id = OBJECT_ID(N'dbo.login_users')
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UQ_login_users_discord_id
    ON dbo.login_users(discord_id)
    WHERE discord_id IS NOT NULL;');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_login_users_discord_user_id'
      AND object_id = OBJECT_ID(N'dbo.login_users')
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UQ_login_users_discord_user_id
    ON dbo.login_users(discord_user_id)
    WHERE discord_user_id IS NOT NULL;');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_users_discord_user_id'
      AND object_id = OBJECT_ID(N'dbo.users')
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UQ_users_discord_user_id
    ON dbo.users(discord_user_id)
    WHERE discord_user_id IS NOT NULL;');
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

IF COL_LENGTH('dbo.audit_logs', 'login_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD login_user_id INT NULL;
END;

IF COL_LENGTH('dbo.audit_logs', 'ip_address') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD ip_address NVARCHAR(64) NULL;
END;

IF COL_LENGTH('dbo.audit_logs', 'user_agent') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD user_agent NVARCHAR(500) NULL;
END;

IF COL_LENGTH('dbo.audit_logs', 'session_id') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD session_id NVARCHAR(120) NULL;
END;

IF COL_LENGTH('dbo.audit_logs', 'device_info') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD device_info NVARCHAR(160) NULL;
END;

IF COL_LENGTH('dbo.audit_logs', 'correlation_id') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs
        ADD correlation_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_audit_logs_correlation_id DEFAULT NEWID();
END;

IF COL_LENGTH('dbo.audit_logs', 'batch_uuid') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs ADD batch_uuid UNIQUEIDENTIFIER NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_audit_logs_login_user'
)
BEGIN
    ALTER TABLE dbo.audit_logs
    ADD CONSTRAINT FK_audit_logs_login_user
    FOREIGN KEY (login_user_id) REFERENCES dbo.login_users(id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_audit_logs_login_user'
      AND object_id = OBJECT_ID(N'dbo.audit_logs')
)
BEGIN
    CREATE INDEX IX_audit_logs_login_user
    ON dbo.audit_logs(login_user_id, created_at);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_audit_logs_correlation_id'
      AND object_id = OBJECT_ID(N'dbo.audit_logs')
)
BEGIN
    CREATE INDEX IX_audit_logs_correlation_id
    ON dbo.audit_logs(correlation_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_audit_logs_batch_uuid'
      AND object_id = OBJECT_ID(N'dbo.audit_logs')
)
BEGIN
    CREATE INDEX IX_audit_logs_batch_uuid
    ON dbo.audit_logs(batch_uuid);
END;
""");

        await db.Database.ExecuteSqlRawAsync("""
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

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.service_items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.service_items
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_service_items PRIMARY KEY,
        uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_service_items_uuid DEFAULT NEWID(),
        seed_key NVARCHAR(80) NOT NULL,
        category NVARCHAR(30) NOT NULL,
        subcategory NVARCHAR(50) NULL,
        name NVARCHAR(100) NOT NULL,
        unit_type NVARCHAR(30) NOT NULL CONSTRAINT DF_service_items_unit_type DEFAULT N'custom',
        default_price DECIMAL(12,2) NULL,
        price_note NVARCHAR(200) NULL,
        remark NVARCHAR(1000) NULL,
        sort_order INT NOT NULL CONSTRAINT DF_service_items_sort_order DEFAULT 0,
        is_active BIT NOT NULL CONSTRAINT DF_service_items_is_active DEFAULT 1,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_service_items_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL,
        CONSTRAINT CK_service_items_default_price CHECK ([default_price] IS NULL OR [default_price] >= 0),
        CONSTRAINT CK_service_items_category CHECK ([category] IN (N'boost', N'grind', N'play', N'special_companion', N'gift', N'deposit_bonus', N'other'))
    );

    CREATE UNIQUE INDEX UQ_service_items_uuid ON dbo.service_items(uuid);
    CREATE UNIQUE INDEX UQ_service_items_seed_key ON dbo.service_items(seed_key);
    CREATE INDEX IX_service_items_category_sort ON dbo.service_items(category, sort_order);
END;

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_service_items_category'
      AND parent_object_id = OBJECT_ID(N'dbo.service_items')
      AND definition NOT LIKE N'%special_companion%'
)
    ALTER TABLE dbo.service_items DROP CONSTRAINT CK_service_items_category;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_service_items_category'
      AND parent_object_id = OBJECT_ID(N'dbo.service_items')
)
    EXEC(N'ALTER TABLE dbo.service_items WITH CHECK ADD CONSTRAINT CK_service_items_category CHECK ([category] IN (N''boost'', N''grind'', N''play'', N''special_companion'', N''gift'', N''deposit_bonus'', N''other''))');

WITH seed_items AS
(
    SELECT *
    FROM (VALUES
        (N'boost-rank', N'boost', N'代打', N'代打 - 段位', N'custom', CAST(NULL AS DECIMAL(12,2)), N'另議', N'段位價格尚未細分，先保留手動輸入。', 100),
        (N'boost-badge', N'boost', N'代打', N'代打 - 牌子', N'custom', CAST(NULL AS DECIMAL(12,2)), N'另議', N'牌子價格尚未細分，先保留手動輸入。', 110),

        (N'grind-weekly-1w', N'grind', N'週上限', N'代肝 - 每 1w', N'week', CAST(50 AS DECIMAL(12,2)), N'50 / 週', N'週末單改每 1w +150。', 200),
        (N'grind-weekly-42w', N'grind', N'週上限', N'代肝 - 4.2w 打滿', N'week', CAST(200 AS DECIMAL(12,2)), N'200 / 週', N'不接週末單。', 210),
        (N'grind-weekly-54w', N'grind', N'週上限', N'代肝 - 5.4w 季末倒數打滿', N'week', CAST(250 AS DECIMAL(12,2)), N'250 / 週', N'季末週末單 +500；不接週日單。', 220),
        (N'grind-rank-daily', N'grind', N'低保', N'代肝 - 排位低保 3 場', N'day', CAST(25 AS DECIMAL(12,2)), N'25 / 日', NULL, 230),
        (N'grind-team-daily', N'grind', N'低保', N'代肝 - 五排低保 3 場', N'day', CAST(20 AS DECIMAL(12,2)), N'20 / 日', N'記得提醒不包贏。', 240),
        (N'grind-weekly-treasures', N'grind', N'週常', N'代肝 - 娛樂週常三珍寶', N'week', CAST(20 AS DECIMAL(12,2)), N'20 / 週', NULL, 250),
        (N'grind-lose-match', N'grind', N'敗場', N'代肝 - 刷敗場', N'match', CAST(8 AS DECIMAL(12,2)), N'8 / 場', N'買 10 送 1。', 260),

        (N'play-entertainment', N'play', N'一般娛樂', N'陪玩 - 娛樂陪', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 300),
        (N'play-technical-tier-1-3', N'play', N'一般排位', N'陪玩 - 排位 1-4 階', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 310),
        (N'play-technical-tier-4', N'play', N'一般排位', N'陪玩 - 排位 5-6 階', N'hour_person', CAST(210 AS DECIMAL(12,2)), N'210 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 320),
        (N'play-technical-tier-5', N'play', N'一般排位', N'陪玩 - 排位 7 階以上', N'hour_person', CAST(260 AS DECIMAL(12,2)), N'260 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 330),
        (N'play-technical-tier-6', N'play', N'舊價目', N'陪玩 - 技術陪 六階', N'hour_person', CAST(230 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 5-6 階。', 340),
        (N'play-technical-tier-7', N'play', N'舊價目', N'陪玩 - 技術陪 七階', N'hour_person', CAST(250 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 350),
        (N'play-technical-tier-peak7', N'play', N'舊價目', N'陪玩 - 技術陪 巔七以上', N'hour_person', CAST(300 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 360),
        (N'play-teaching', N'play', N'舊價目', N'陪玩 - 教學陪', N'hour_person', CAST(100 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列教學陪。', 370),
        (N'play-gold-entertainment', N'play', N'摸金／加頁', N'加頁 - 娛樂', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 380),
        (N'play-gold-technical', N'play', N'摸金／加頁', N'加頁 - 技術', N'hour_person', CAST(170 AS DECIMAL(12,2)), N'170 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 390),
        (N'play-gold-demon-protect', N'play', N'摸金／加頁', N'加頁 - 魔王護', N'hour_person', CAST(360 AS DECIMAL(12,2)), N'360 / 小時', N'二陪一，主打提高撤離保障與陪同強度。', 395),

        (N'gift-candle', N'gift', N'禮物', N'香氛蠟燭', N'item', CAST(40 AS DECIMAL(12,2)), N'40', NULL, 400),
        (N'gift-star-bottle', N'gift', N'禮物', N'星空瓶', N'item', CAST(100 AS DECIMAL(12,2)), N'100', N'冠名顯示一天；可設定專屬稱呼，禁止奇怪暱稱。', 410),
        (N'gift-candy-jar', N'gift', N'禮物', N'糖果罐', N'item', CAST(250 AS DECIMAL(12,2)), N'250', N'冠名顯示三天；專屬稱呼。', 420),
        (N'gift-love-breakfast', N'gift', N'禮物', N'愛心早餐', N'item', CAST(520 AS DECIMAL(12,2)), N'520', N'冠名顯示七天；專屬頭像；專屬稱呼；一張限時一週 95 折卡。', 430),
        (N'gift-deer-pillow', N'gift', N'禮物', N'小鹿抱枕', N'item', CAST(888 AS DECIMAL(12,2)), N'888', N'冠名顯示十天；專屬稱呼；一張限時一週 9 折卡；專屬頭像；可指定專屬小互動。', 440),
        (N'gift-basque-cake', N'gift', N'禮物', N'巴斯克蛋糕', N'item', CAST(1314 AS DECIMAL(12,2)), N'1314', N'冠名顯示十五天；專屬頭像；專屬稱呼；一張限時一週 9 折卡；專屬個人身份組；可指定專屬小互動；專屬語音條 30 秒以內。', 450),

        (N'deposit-bonus-1000', N'deposit_bonus', N'預存', N'預存滿 1000 加贈 100', N'amount', CAST(100 AS DECIMAL(12,2)), N'存 1000 得 1100', N'第一次預存滿 1000 即可享下單九折；每滿 1000 加贈 100 購物金。', 500),
        (N'deposit-bonus-5000', N'deposit_bonus', N'預存', N'預存滿 5000 加贈金額 x2', N'amount', CAST(NULL AS DECIMAL(12,2)), N'存 5000 得 6000', N'預存 5000 以上加贈金額直接 x2。', 510),
        (N'deposit-bonus-10000', N'deposit_bonus', N'預存', N'預存滿 10000 以上福利另議', N'amount', CAST(NULL AS DECIMAL(12,2)), N'另議', N'預存滿 10000 以上另有額外福利可私訊討論。', 520)
    ) AS v(seed_key, category, subcategory, name, unit_type, default_price, price_note, remark, sort_order)
)
INSERT INTO dbo.service_items
(
    seed_key,
    category,
    subcategory,
    name,
    unit_type,
    default_price,
    price_note,
    remark,
    sort_order,
    is_active,
    created_at
)
SELECT
    s.seed_key,
    s.category,
    s.subcategory,
    s.name,
    s.unit_type,
    s.default_price,
    s.price_note,
    s.remark,
    s.sort_order,
    1,
    SYSUTCDATETIME()
FROM seed_items s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.service_items existing
    WHERE existing.seed_key = s.seed_key
)
AND COL_LENGTH('dbo.service_items', 'organization_id') IS NULL;

UPDATE dbo.service_items
SET
    is_active = 0,
    updated_at = SYSUTCDATETIME(),
    remark = N'已改用分階段位代打價目。'
WHERE seed_key = N'boost-rank';

UPDATE dbo.service_items
SET
    remark = N'不接週末單。',
    updated_at = SYSUTCDATETIME()
WHERE seed_key = N'grind-weekly-42w';

UPDATE dbo.service_items
SET
    remark = N'季末週末單 +500；不接週日單。',
    updated_at = SYSUTCDATETIME()
WHERE seed_key = N'grind-weekly-54w';

UPDATE dbo.service_items
SET
    subcategory = N'角色代打',
    name = N'代打 - 角色代打 / 牌子',
    unit_type = N'custom',
    default_price = NULL,
    price_note = N'另議',
    remark = N'由打手開價；若覺得價格不合可自行溝通。',
    sort_order = 160,
    updated_at = SYSUTCDATETIME()
WHERE seed_key = N'boost-badge';

WITH boost_rank_items AS
(
    SELECT *
    FROM (VALUES
        (N'boost-rank-tier-1-3', N'boost', N'段位', N'代打 - 段位 1-3 階', N'star', CAST(20 AS DECIMAL(12,2)), N'20 / 星', N'求生 / 監管代打。', 120),
        (N'boost-rank-tier-3-4', N'boost', N'段位', N'代打 - 段位 3-4 階', N'star', CAST(40 AS DECIMAL(12,2)), N'40 / 星', N'求生 / 監管代打。', 130),
        (N'boost-rank-tier-4-5', N'boost', N'段位', N'代打 - 段位 4-5 階', N'star', CAST(60 AS DECIMAL(12,2)), N'60 / 星', N'求生 / 監管代打。', 140),
        (N'boost-rank-tier-5-6', N'boost', N'段位', N'代打 - 段位 5-6 階', N'star', CAST(100 AS DECIMAL(12,2)), N'100 / 星', N'求生 / 監管代打。', 150),
        (N'boost-rank-tier-7', N'boost', N'段位', N'代打 - 段位 7 階', N'star', CAST(110 AS DECIMAL(12,2)), N'110 / 星', N'求生 / 監管代打。', 155)
    ) AS v(seed_key, category, subcategory, name, unit_type, default_price, price_note, remark, sort_order)
)
INSERT INTO dbo.service_items
(
    seed_key,
    category,
    subcategory,
    name,
    unit_type,
    default_price,
    price_note,
    remark,
    sort_order,
    is_active,
    created_at
)
SELECT
    s.seed_key,
    s.category,
    s.subcategory,
    s.name,
    s.unit_type,
    s.default_price,
    s.price_note,
    s.remark,
    s.sort_order,
    1,
    SYSUTCDATETIME()
FROM boost_rank_items s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.service_items existing
    WHERE existing.seed_key = s.seed_key
)
AND COL_LENGTH('dbo.service_items', 'organization_id') IS NULL;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.gift_records', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.gift_records
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_gift_records PRIMARY KEY,
        uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_gift_records_uuid DEFAULT NEWID(),
        gift_date DATE NOT NULL,
        boss_user_id INT NOT NULL,
        recipient_user_id INT NOT NULL,
        service_item_id INT NULL,
        gift_name NVARCHAR(100) NOT NULL,
        amount DECIMAL(12,2) NOT NULL,
        quantity DECIMAL(12,2) NOT NULL CONSTRAINT DF_gift_records_quantity DEFAULT 1,
        customer_payment_status NVARCHAR(20) NOT NULL CONSTRAINT DF_gift_records_customer_payment_status DEFAULT N'unpaid',
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_gift_records_status DEFAULT N'completed',
        remark NVARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_gift_records_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL,
        CONSTRAINT CK_gift_records_amount CHECK ([amount] > 0),
        CONSTRAINT CK_gift_records_quantity CHECK ([quantity] > 0),
        CONSTRAINT CK_gift_records_customer_payment_status CHECK ([customer_payment_status] IN (N'unpaid', N'partial', N'paid', N'refunded')),
        CONSTRAINT CK_gift_records_status CHECK ([status] IN (N'completed', N'cancelled')),
        CONSTRAINT FK_gift_records_boss_user FOREIGN KEY (boss_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_gift_records_recipient_user FOREIGN KEY (recipient_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_gift_records_service_item FOREIGN KEY (service_item_id) REFERENCES dbo.service_items(id)
    );

    CREATE UNIQUE INDEX UQ_gift_records_uuid ON dbo.gift_records(uuid);
    CREATE INDEX IX_gift_records_date_status ON dbo.gift_records(gift_date, status);
    CREATE INDEX IX_gift_records_boss_date ON dbo.gift_records(boss_user_id, gift_date);
    CREATE INDEX IX_gift_records_recipient_date ON dbo.gift_records(recipient_user_id, gift_date);
END;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.departments
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_departments PRIMARY KEY,
        uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_departments_uuid DEFAULT NEWID(),
        name NVARCHAR(50) NOT NULL,
        english_name NVARCHAR(80) NULL,
        description NVARCHAR(1000) NULL,
        sort_order INT NOT NULL CONSTRAINT DF_departments_sort_order DEFAULT 0,
        is_active BIT NOT NULL CONSTRAINT DF_departments_is_active DEFAULT 1,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_departments_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL
    );

    CREATE UNIQUE INDEX UQ_departments_uuid ON dbo.departments(uuid);
    CREATE UNIQUE INDEX UQ_departments_name ON dbo.departments(name);
    CREATE INDEX IX_departments_sort ON dbo.departments(sort_order, name);
END;

IF OBJECT_ID(N'dbo.department_members', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.department_members
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_department_members PRIMARY KEY,
        department_id INT NOT NULL,
        user_id INT NOT NULL,
        position_title NVARCHAR(80) NULL,
        is_manager BIT NOT NULL CONSTRAINT DF_department_members_is_manager DEFAULT 0,
        joined_at DATETIME2 NOT NULL CONSTRAINT DF_department_members_joined_at DEFAULT SYSUTCDATETIME(),
        left_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_department_members_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_department_members_department FOREIGN KEY (department_id) REFERENCES dbo.departments(id) ON DELETE CASCADE,
        CONSTRAINT FK_department_members_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );

    CREATE UNIQUE INDEX UQ_department_members_active
    ON dbo.department_members(department_id, user_id)
    WHERE left_at IS NULL;
    CREATE INDEX IX_department_members_user ON dbo.department_members(user_id, department_id);
END;

WITH seed_departments AS
(
    SELECT *
    FROM (VALUES
        (N'管理層', N'Management', N'制定營運方向；價格策略與財務審核；主管招募與危機處理；對外合作決策。', 100),
        (N'營運部', N'Operations', N'訂單管理；排班調度；加單、取消訂單處理；服務品質控管；客訴與黑名單管理。', 200),
        (N'人資部', N'HR', N'招募與面試；新人培訓；停權、退團管理。', 300),
        (N'客服部', N'Customer Service', N'售前報價與推薦；售中協調時間與更換人員；售後糾紛、退款、補單處理。', 400),
        (N'陪玩部', N'Playmate', N'接單與陪玩服務；客戶互動維護；服務回報。', 500),
        (N'財務部', N'Finance', N'收款與匯款；薪資結算；抽成計算。', 600),
        (N'行銷部', N'Marketing', N'社群經營；廣告投放；活動企劃；短影音製作；數據追蹤分析。', 700),
        (N'美術設計部', N'Design', N'品牌視覺設計；陪玩師介紹卡；海報與宣傳素材；影片剪輯；LOGO 與吉祥物設計。', 800),
        (N'資訊部', N'IT', N'ERP 系統開發；Discord Bot 開發；官網維護；資料庫管理；伺服器與備份；流程自動化。', 900),
        (N'品管部', N'QA', N'服務品質稽核；抽查語音與聊天紀錄；客戶評價追蹤；違規管理。', 1000),
        (N'商務部', N'Business Development', N'實況主合作；VTuber 合作；公會／戰隊合作；聯名活動；分潤方案；推廣碼規劃。', 1100),
        (N'數據分析部', N'BI', N'客戶數量、回購率、客單價、留存率分析；陪玩師接單率、好評率、平均時薪、熱門角色排行分析。', 1200)
    ) AS v(name, english_name, description, sort_order)
)
INSERT INTO dbo.departments
(
    name,
    english_name,
    description,
    sort_order,
    is_active,
    created_at
)
SELECT
    s.name,
    s.english_name,
    s.description,
    s.sort_order,
    1,
    SYSUTCDATETIME()
FROM seed_departments s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.departments existing
    WHERE existing.name = s.name
)
AND COL_LENGTH('dbo.departments', 'organization_id') IS NULL;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.organizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.organizations
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_organizations PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        is_active BIT NOT NULL CONSTRAINT DF_organizations_is_active DEFAULT 1,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_organizations_created_at DEFAULT SYSUTCDATETIME()
    );
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_organizations_name' AND object_id = OBJECT_ID(N'dbo.organizations'))
    DROP INDEX UQ_organizations_name ON dbo.organizations;

IF NOT EXISTS (SELECT 1 FROM dbo.organizations)
BEGIN
    INSERT INTO dbo.organizations(name, is_active, created_at)
    VALUES (N'Playmate Taipei', 1, SYSUTCDATETIME());
END;

DECLARE @default_organization_id INT = (SELECT TOP (1) id FROM dbo.organizations ORDER BY id);

IF COL_LENGTH('dbo.login_users', 'organization_id') IS NULL
    ALTER TABLE dbo.login_users ADD organization_id INT NULL;
IF COL_LENGTH('dbo.login_users', 'user_id') IS NULL
    ALTER TABLE dbo.login_users ADD user_id INT NULL;
IF COL_LENGTH('dbo.users', 'organization_id') IS NULL
    ALTER TABLE dbo.users ADD organization_id INT NULL;
IF COL_LENGTH('dbo.orders', 'organization_id') IS NULL
    ALTER TABLE dbo.orders ADD organization_id INT NULL;
IF COL_LENGTH('dbo.order_members', 'organization_id') IS NULL
    ALTER TABLE dbo.order_members ADD organization_id INT NULL;
IF COL_LENGTH('dbo.payments', 'organization_id') IS NULL
    ALTER TABLE dbo.payments ADD organization_id INT NULL;
IF COL_LENGTH('dbo.audit_logs', 'organization_id') IS NULL
    ALTER TABLE dbo.audit_logs ADD organization_id INT NULL;
IF COL_LENGTH('dbo.service_items', 'organization_id') IS NULL
    ALTER TABLE dbo.service_items ADD organization_id INT NULL;
IF COL_LENGTH('dbo.gift_records', 'organization_id') IS NULL
    ALTER TABLE dbo.gift_records ADD organization_id INT NULL;
IF COL_LENGTH('dbo.departments', 'organization_id') IS NULL
    ALTER TABLE dbo.departments ADD organization_id INT NULL;
IF COL_LENGTH('dbo.department_members', 'organization_id') IS NULL
    ALTER TABLE dbo.department_members ADD organization_id INT NULL;
""");

        await db.Database.ExecuteSqlRawAsync("""
DECLARE @default_organization_id INT = (SELECT TOP (1) id FROM dbo.organizations ORDER BY id);
UPDATE dbo.login_users SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE lu
SET user_id = u.id
FROM dbo.login_users lu
INNER JOIN dbo.users u ON u.login_account = lu.login_account
WHERE lu.user_id IS NULL;
UPDATE u
SET discord_user_id = lu.discord_user_id,
    discord_id = lu.discord_id,
    discord_name = lu.discord_name
FROM dbo.users u
INNER JOIN dbo.login_users lu ON lu.user_id = u.id
WHERE lu.discord_linked_at IS NOT NULL
  AND lu.discord_user_id IS NOT NULL;
UPDATE lu
SET discord_id = u.discord_id,
    discord_name = u.discord_name
FROM dbo.login_users lu
INNER JOIN dbo.users u ON u.id = lu.user_id
WHERE lu.discord_id IS NULL
  AND u.discord_id IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.login_users existing
      WHERE existing.discord_id = u.discord_id
  );
UPDATE dbo.users SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dbo.orders SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE om
SET organization_id = o.organization_id
FROM dbo.order_members om
INNER JOIN dbo.orders o ON o.id = om.order_id
WHERE om.organization_id IS NULL;
UPDATE dbo.payments SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dbo.audit_logs SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dbo.service_items SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dbo.gift_records SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dbo.departments SET organization_id = @default_organization_id WHERE organization_id IS NULL;
UPDATE dm
SET organization_id = d.organization_id
FROM dbo.department_members dm
INNER JOIN dbo.departments d ON d.id = dm.department_id
WHERE dm.organization_id IS NULL;

ALTER TABLE dbo.login_users ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.users ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.orders ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.order_members ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.payments ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.audit_logs ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.service_items ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.gift_records ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.departments ALTER COLUMN organization_id INT NOT NULL;
ALTER TABLE dbo.department_members ALTER COLUMN organization_id INT NOT NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE name = N'UQ_users_nickname'
      AND parent_object_id = OBJECT_ID(N'dbo.users')
)
    ALTER TABLE dbo.users DROP CONSTRAINT UQ_users_nickname;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_users_nickname' AND object_id = OBJECT_ID(N'dbo.users'))
    DROP INDEX UQ_users_nickname ON dbo.users;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_users_organization_nickname' AND object_id = OBJECT_ID(N'dbo.users'))
    CREATE UNIQUE INDEX UQ_users_organization_nickname ON dbo.users(organization_id, nickname);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_service_items_seed_key' AND object_id = OBJECT_ID(N'dbo.service_items'))
    DROP INDEX UQ_service_items_seed_key ON dbo.service_items;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_service_items_organization_seed_key' AND object_id = OBJECT_ID(N'dbo.service_items'))
    CREATE UNIQUE INDEX UQ_service_items_organization_seed_key ON dbo.service_items(organization_id, seed_key);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_departments_name' AND object_id = OBJECT_ID(N'dbo.departments'))
    DROP INDEX UQ_departments_name ON dbo.departments;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_departments_organization_name' AND object_id = OBJECT_ID(N'dbo.departments'))
    CREATE UNIQUE INDEX UQ_departments_organization_name ON dbo.departments(organization_id, name);
""");

        await db.Database.ExecuteSqlRawAsync("""
DECLARE @default_organization_id INT = (SELECT TOP (1) id FROM dbo.organizations ORDER BY id);

WITH restore_departments AS
(
    SELECT *
    FROM (VALUES
        (N'管理層', N'Management', N'制定營運方向；價格策略與財務審核；主管招募與危機處理；對外合作決策。', 100),
        (N'營運部', N'Operations', N'訂單管理；排班調度；加單、取消訂單處理；服務品質控管；客訴與黑名單管理。', 200),
        (N'人資部', N'HR', N'招募與面試；新人培訓；停權、退團管理。', 300),
        (N'客服部', N'Customer Service', N'售前報價與推薦；售中協調時間與更換人員；售後聯絡、退款、補單處理。', 400),
        (N'陪玩部', N'Playmate', N'接單與陪玩服務；客戶互動維護；服務回報。', 500),
        (N'財務部', N'Finance', N'收款與匯款；薪資結算；抽成計算。', 600),
        (N'行銷部', N'Marketing', N'社群經營；公告投放；活動企劃；短影音製作；數據追蹤分析。', 700),
        (N'美術設計部', N'Design', N'品牌視覺設計；陪玩介紹卡；海報與宣傳素材；影片剪輯；LOGO 與吉祥物設計。', 800),
        (N'資訊部', N'IT', N'ERP 系統開發；Discord Bot 開發；官網維護；資料庫管理；伺服器與備份；流程自動化。', 900),
        (N'品管部', N'QA', N'服務品質稽核；抽查語音與聊天紀錄；客戶評價追蹤；違規管理。', 1000),
        (N'商務部', N'Business Development', N'實況主合作；VTuber 合作；公會、戰隊合作；聯名活動；分潤方案；推薦碼規劃。', 1100),
        (N'數據分析部', N'BI', N'客戶數量、回購率、客單價、留存率分析；陪玩師接單率、好評率、平均時薪、熱門角色排行分析。', 1200)
    ) AS v(name, english_name, description, sort_order)
)
INSERT INTO dbo.departments
(
    organization_id,
    name,
    english_name,
    description,
    sort_order,
    is_active,
    created_at
)
SELECT
    @default_organization_id,
    s.name,
    s.english_name,
    s.description,
    s.sort_order,
    1,
    SYSUTCDATETIME()
FROM restore_departments s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.departments existing
    WHERE existing.name = s.name
);
""");

        await db.Database.ExecuteSqlRawAsync("""
DECLARE @default_organization_id INT = (SELECT TOP (1) id FROM dbo.organizations ORDER BY id);

WITH restore_service_items AS
(
    SELECT *
    FROM (VALUES
        (N'boost-rank', N'boost', N'代打', N'代打 - 段位', N'custom', CAST(NULL AS DECIMAL(12,2)), N'另議', N'已改用分階段位代打價目。', 100, CAST(0 AS BIT)),
        (N'boost-badge', N'boost', N'角色代打', N'代打 - 角色代打 / 牌子', N'custom', CAST(NULL AS DECIMAL(12,2)), N'另議', N'由打手開價；若覺得價格不合可自行溝通。', 160, CAST(1 AS BIT)),

        (N'boost-rank-tier-1-3', N'boost', N'段位', N'代打 - 段位 1-3 階', N'star', CAST(20 AS DECIMAL(12,2)), N'20 / 星', N'求生 / 監管代打。', 120, CAST(1 AS BIT)),
        (N'boost-rank-tier-3-4', N'boost', N'段位', N'代打 - 段位 3-4 階', N'star', CAST(40 AS DECIMAL(12,2)), N'40 / 星', N'求生 / 監管代打。', 130, CAST(1 AS BIT)),
        (N'boost-rank-tier-4-5', N'boost', N'段位', N'代打 - 段位 4-5 階', N'star', CAST(60 AS DECIMAL(12,2)), N'60 / 星', N'求生 / 監管代打。', 140, CAST(1 AS BIT)),
        (N'boost-rank-tier-5-6', N'boost', N'段位', N'代打 - 段位 5-6 階', N'star', CAST(100 AS DECIMAL(12,2)), N'100 / 星', N'求生 / 監管代打。', 150, CAST(1 AS BIT)),
        (N'boost-rank-tier-7', N'boost', N'段位', N'代打 - 段位 7 階', N'star', CAST(110 AS DECIMAL(12,2)), N'110 / 星', N'求生 / 監管代打。', 155, CAST(1 AS BIT)),

        (N'grind-weekly-1w', N'grind', N'週上限', N'代肝 - 每 1w', N'week', CAST(50 AS DECIMAL(12,2)), N'50 / 週', N'週末單改每 1w +150。', 200, CAST(1 AS BIT)),
        (N'grind-weekly-42w', N'grind', N'週上限', N'代肝 - 4.2w 打滿', N'week', CAST(200 AS DECIMAL(12,2)), N'200 / 週', N'不接週末單。', 210, CAST(1 AS BIT)),
        (N'grind-weekly-54w', N'grind', N'週上限', N'代肝 - 5.4w 季末倒數打滿', N'week', CAST(250 AS DECIMAL(12,2)), N'250 / 週', N'季末週末單 +500；不接週日單。', 220, CAST(1 AS BIT)),
        (N'grind-rank-daily', N'grind', N'低保', N'代肝 - 排位低保 3 場', N'day', CAST(25 AS DECIMAL(12,2)), N'25 / 日', NULL, 230, CAST(1 AS BIT)),
        (N'grind-team-daily', N'grind', N'低保', N'代肝 - 五排低保 3 場', N'day', CAST(20 AS DECIMAL(12,2)), N'20 / 日', N'記得提醒不包贏。', 240, CAST(1 AS BIT)),
        (N'grind-weekly-treasures', N'grind', N'週常', N'代肝 - 娛樂週常三珍寶', N'week', CAST(20 AS DECIMAL(12,2)), N'20 / 週', NULL, 250, CAST(1 AS BIT)),
        (N'grind-lose-match', N'grind', N'敗場', N'代肝 - 刷敗場', N'match', CAST(8 AS DECIMAL(12,2)), N'8 / 場', N'買 10 送 1。', 260, CAST(1 AS BIT)),

        (N'play-entertainment', N'play', N'一般娛樂', N'陪玩 - 娛樂陪', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 300, CAST(1 AS BIT)),
        (N'play-technical-tier-1-3', N'play', N'一般排位', N'陪玩 - 排位 1-4 階', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 310, CAST(1 AS BIT)),
        (N'play-technical-tier-4', N'play', N'一般排位', N'陪玩 - 排位 5-6 階', N'hour_person', CAST(210 AS DECIMAL(12,2)), N'210 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 320, CAST(1 AS BIT)),
        (N'play-technical-tier-5', N'play', N'一般排位', N'陪玩 - 排位 7 階以上', N'hour_person', CAST(260 AS DECIMAL(12,2)), N'260 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 330, CAST(1 AS BIT)),
        (N'play-technical-tier-6', N'play', N'舊價目', N'陪玩 - 技術陪 六階', N'hour_person', CAST(230 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 5-6 階。', 340, CAST(0 AS BIT)),
        (N'play-technical-tier-7', N'play', N'舊價目', N'陪玩 - 技術陪 七階', N'hour_person', CAST(250 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 350, CAST(0 AS BIT)),
        (N'play-technical-tier-peak7', N'play', N'舊價目', N'陪玩 - 技術陪 巔七以上', N'hour_person', CAST(300 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 360, CAST(0 AS BIT)),
        (N'play-teaching', N'play', N'舊價目', N'陪玩 - 教學陪', N'hour_person', CAST(100 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列教學陪。', 370, CAST(0 AS BIT)),
        (N'play-gold-entertainment', N'play', N'摸金／加頁', N'加頁 - 娛樂', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 380, CAST(1 AS BIT)),
        (N'play-gold-technical', N'play', N'摸金／加頁', N'加頁 - 技術', N'hour_person', CAST(170 AS DECIMAL(12,2)), N'170 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 390, CAST(1 AS BIT)),
        (N'play-gold-demon-protect', N'play', N'摸金／加頁', N'加頁 - 魔王護', N'hour_person', CAST(360 AS DECIMAL(12,2)), N'360 / 小時', N'二陪一，主打提高撤離保障與陪同強度。', 395, CAST(1 AS BIT)),

        (N'gift-candle', N'gift', N'禮物', N'香氛蠟燭', N'item', CAST(40 AS DECIMAL(12,2)), N'40', NULL, 400, CAST(1 AS BIT)),
        (N'gift-star-bottle', N'gift', N'禮物', N'星空瓶', N'item', CAST(100 AS DECIMAL(12,2)), N'100', N'冠名顯示一天；可設定專屬稱呼，禁止奇怪暱稱。', 410, CAST(1 AS BIT)),
        (N'gift-candy-jar', N'gift', N'禮物', N'糖果罐', N'item', CAST(250 AS DECIMAL(12,2)), N'250', N'冠名顯示三天；專屬稱呼。', 420, CAST(1 AS BIT)),
        (N'gift-love-breakfast', N'gift', N'禮物', N'愛心早餐', N'item', CAST(520 AS DECIMAL(12,2)), N'520', N'冠名顯示七天；專屬頭像；專屬稱呼；一張限時一週 95 折卡。', 430, CAST(1 AS BIT)),
        (N'gift-deer-pillow', N'gift', N'禮物', N'小鹿抱枕', N'item', CAST(888 AS DECIMAL(12,2)), N'888', N'冠名顯示十天；專屬稱呼；一張限時一週 9 折卡；專屬頭像；可指定專屬小互動。', 440, CAST(1 AS BIT)),
        (N'gift-basque-cake', N'gift', N'禮物', N'巴斯克蛋糕', N'item', CAST(1314 AS DECIMAL(12,2)), N'1314', N'冠名顯示十五天；專屬頭像；專屬稱呼；一張限時一週 9 折卡；專屬個人身分組；可指定專屬小互動；專屬語音條 30 秒以內。', 450, CAST(1 AS BIT)),

        (N'deposit-bonus-1000', N'deposit_bonus', N'預存', N'預存滿 1000 加贈 100', N'amount', CAST(100 AS DECIMAL(12,2)), N'存 1000 得 1100', N'第一次預存滿 1000 即可享下單九折；每滿 1000 加贈 100 贈物金。', 500, CAST(1 AS BIT)),
        (N'deposit-bonus-5000', N'deposit_bonus', N'預存', N'預存滿 5000 加贈金額 x2', N'amount', CAST(NULL AS DECIMAL(12,2)), N'存 5000 得 6000', N'預存 5000 以上加贈金額直接 x2。', 510, CAST(1 AS BIT)),
        (N'deposit-bonus-10000', N'deposit_bonus', N'預存', N'預存滿 10000 以上福利另議', N'amount', CAST(NULL AS DECIMAL(12,2)), N'另議', N'預存滿 10000 以上另有額外福利可私訊討論。', 520, CAST(1 AS BIT))
    ) AS v(seed_key, category, subcategory, name, unit_type, default_price, price_note, remark, sort_order, is_active)
)
INSERT INTO dbo.service_items
(
    organization_id,
    seed_key,
    category,
    subcategory,
    name,
    unit_type,
    default_price,
    price_note,
    remark,
    sort_order,
    is_active,
    created_at
)
SELECT
    @default_organization_id,
    s.seed_key,
    s.category,
    s.subcategory,
    s.name,
    s.unit_type,
    s.default_price,
    s.price_note,
    s.remark,
    s.sort_order,
    s.is_active,
    SYSUTCDATETIME()
FROM restore_service_items s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.service_items existing
    WHERE existing.seed_key = s.seed_key
);
""");

        await db.Database.ExecuteSqlRawAsync("""
DECLARE @default_organization_id INT = (SELECT TOP (1) id FROM dbo.organizations ORDER BY id);

WITH special_companion_items AS
(
    SELECT *
    FROM (VALUES
        (N'special-companion-singing', N'special_companion', N'歌陪', N'歌陪 - 1 小時', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時', N'歌陪 30 分鐘為 100。', 600),
        (N'special-companion-singing-half', N'special_companion', N'歌陪', N'歌陪 - 30 分鐘', N'custom', CAST(100 AS DECIMAL(12,2)), N'100 / 30 分鐘', NULL, 605),
        (N'special-companion-text', N'special_companion', N'小尬劇', N'小尬劇', N'item', CAST(20 AS DECIMAL(12,2)), N'20 / 張', NULL, 610),
        (N'special-companion-punching-bag', N'special_companion', N'舊價目', N'特殊陪 - 受氣包', N'hour_person', CAST(150 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列受氣包。', 620),
        (N'special-companion-voice', N'special_companion', N'語聊', N'語聊', N'hour_person', CAST(130 AS DECIMAL(12,2)), N'130 / 小時', NULL, 630),
        (N'special-companion-sleep', N'special_companion', N'舊價目', N'特殊陪 - 哄睡陪', N'hour_person', CAST(150 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列哄睡陪。', 640)
    ) AS v(seed_key, category, subcategory, name, unit_type, default_price, price_note, remark, sort_order)
)
INSERT INTO dbo.service_items
(
    organization_id,
    seed_key,
    category,
    subcategory,
    name,
    unit_type,
    default_price,
    price_note,
    remark,
    sort_order,
    is_active,
    created_at
)
SELECT
    @default_organization_id,
    s.seed_key,
    s.category,
    s.subcategory,
    s.name,
    s.unit_type,
    s.default_price,
    s.price_note,
    s.remark,
    s.sort_order,
    1,
    SYSUTCDATETIME()
FROM special_companion_items s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.service_items existing
    WHERE existing.seed_key = s.seed_key
);

WITH pilot_service_prices AS
(
    SELECT *
    FROM (VALUES
        (N'play-entertainment', N'play', N'一般娛樂', N'陪玩 - 娛樂陪', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 300, CAST(1 AS BIT)),
        (N'play-technical-tier-1-3', N'play', N'一般排位', N'陪玩 - 排位 1-4 階', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 310, CAST(1 AS BIT)),
        (N'play-technical-tier-4', N'play', N'一般排位', N'陪玩 - 排位 5-6 階', N'hour_person', CAST(210 AS DECIMAL(12,2)), N'210 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 320, CAST(1 AS BIT)),
        (N'play-technical-tier-5', N'play', N'一般排位', N'陪玩 - 排位 7 階以上', N'hour_person', CAST(260 AS DECIMAL(12,2)), N'260 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 330, CAST(1 AS BIT)),
        (N'play-technical-tier-6', N'play', N'舊價目', N'陪玩 - 技術陪 六階', N'hour_person', CAST(230 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 5-6 階。', 340, CAST(0 AS BIT)),
        (N'play-technical-tier-7', N'play', N'舊價目', N'陪玩 - 技術陪 七階', N'hour_person', CAST(250 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 350, CAST(0 AS BIT)),
        (N'play-technical-tier-peak7', N'play', N'舊價目', N'陪玩 - 技術陪 巔七以上', N'hour_person', CAST(300 AS DECIMAL(12,2)), N'已停用', N'新版試行價已改用排位 7 階以上。', 360, CAST(0 AS BIT)),
        (N'play-teaching', N'play', N'舊價目', N'陪玩 - 教學陪', N'hour_person', CAST(100 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列教學陪。', 370, CAST(0 AS BIT)),
        (N'play-gold-entertainment', N'play', N'摸金／加頁', N'加頁 - 娛樂', N'hour_person', CAST(160 AS DECIMAL(12,2)), N'160 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 380, CAST(1 AS BIT)),
        (N'play-gold-technical', N'play', N'摸金／加頁', N'加頁 - 技術', N'hour_person', CAST(170 AS DECIMAL(12,2)), N'170 / 小時 / 人', N'指定陪陪 +20 / 位；帶朋友 +20 / 位；深夜 00:00-06:00 +30。', 390, CAST(1 AS BIT)),
        (N'play-gold-demon-protect', N'play', N'摸金／加頁', N'加頁 - 魔王護', N'hour_person', CAST(360 AS DECIMAL(12,2)), N'360 / 小時', N'二陪一，主打提高撤離保障與陪同強度。', 395, CAST(1 AS BIT)),
        (N'special-companion-singing', N'special_companion', N'歌陪', N'歌陪 - 1 小時', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時', N'歌陪 30 分鐘為 100。', 600, CAST(1 AS BIT)),
        (N'special-companion-singing-half', N'special_companion', N'歌陪', N'歌陪 - 30 分鐘', N'custom', CAST(100 AS DECIMAL(12,2)), N'100 / 30 分鐘', NULL, 605, CAST(1 AS BIT)),
        (N'special-companion-text', N'special_companion', N'小尬劇', N'小尬劇', N'item', CAST(20 AS DECIMAL(12,2)), N'20 / 張', NULL, 610, CAST(1 AS BIT)),
        (N'special-companion-punching-bag', N'special_companion', N'舊價目', N'特殊陪 - 受氣包', N'hour_person', CAST(150 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列受氣包。', 620, CAST(0 AS BIT)),
        (N'special-companion-voice', N'special_companion', N'語聊', N'語聊', N'hour_person', CAST(130 AS DECIMAL(12,2)), N'130 / 小時', NULL, 630, CAST(1 AS BIT)),
        (N'special-companion-sleep', N'special_companion', N'舊價目', N'特殊陪 - 哄睡陪', N'hour_person', CAST(150 AS DECIMAL(12,2)), N'已停用', N'新版試行價暫不列哄睡陪。', 640, CAST(0 AS BIT))
    ) AS v(seed_key, category, subcategory, name, unit_type, default_price, price_note, remark, sort_order, is_active)
)
UPDATE target
SET
    category = source.category,
    subcategory = source.subcategory,
    name = source.name,
    unit_type = source.unit_type,
    default_price = source.default_price,
    price_note = source.price_note,
    remark = source.remark,
    sort_order = source.sort_order,
    is_active = source.is_active,
    updated_at = SYSUTCDATETIME()
FROM dbo.service_items target
INNER JOIN pilot_service_prices source ON source.seed_key = target.seed_key;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.audit_logs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.login_users', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.audit_logs', 'organization_id') IS NOT NULL
   AND COL_LENGTH('dbo.audit_logs', 'login_user_id') IS NOT NULL
   AND COL_LENGTH('dbo.login_users', 'organization_id') IS NOT NULL
BEGIN
    UPDATE a
    SET organization_id = lu.organization_id
    FROM dbo.audit_logs a
    INNER JOIN dbo.login_users lu ON lu.id = a.login_user_id
    WHERE a.organization_id <> lu.organization_id;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.columns c ON c.object_id = t.object_id
    WHERE t.name IN
    (
        N'login_users', N'users', N'orders', N'order_members', N'payments',
        N'audit_logs', N'service_items', N'gift_records', N'departments',
        N'department_members'
    )
      AND c.name = N'organization_id'
      AND c.is_nullable = 1
)
    THROW 51000, 'Organization schema validation failed: organization_id must be NOT NULL.', 1;

IF EXISTS
(
    SELECT 1 FROM dbo.login_users WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.users WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.orders WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.order_members WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.payments WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.audit_logs WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.service_items WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.activities WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.gift_records WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.departments WHERE organization_id <= 0
    UNION ALL SELECT 1 FROM dbo.department_members WHERE organization_id <= 0
)
    THROW 51001, 'Organization data validation failed: organization_id is missing or invalid.', 1;

IF EXISTS
(
    SELECT 1 FROM dbo.login_users x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.users x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.orders x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.order_members x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.payments x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.audit_logs x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.service_items x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.activities x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.gift_records x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.departments x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
    UNION ALL SELECT 1 FROM dbo.department_members x LEFT JOIN dbo.organizations o ON o.id = x.organization_id WHERE o.id IS NULL
)
    THROW 51007, 'Organization data validation failed: organization_id references a missing organization.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.login_users lu
    INNER JOIN dbo.users u ON u.id = lu.user_id
    WHERE lu.organization_id <> u.organization_id
)
    THROW 51002, 'Organization data validation failed: login user and member organizations differ.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.order_members om
    INNER JOIN dbo.orders o ON o.id = om.order_id
    INNER JOIN dbo.users u ON u.id = om.user_id
    WHERE om.organization_id <> o.organization_id
       OR om.organization_id <> u.organization_id
)
    THROW 51003, 'Organization data validation failed: order member organization mismatch.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.department_members dm
    INNER JOIN dbo.departments d ON d.id = dm.department_id
    INNER JOIN dbo.users u ON u.id = dm.user_id
    WHERE dm.organization_id <> d.organization_id
       OR dm.organization_id <> u.organization_id
)
    THROW 51004, 'Organization data validation failed: department member organization mismatch.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.payments p
    INNER JOIN dbo.users u ON u.id = p.user_id
    WHERE p.organization_id <> u.organization_id
)
    THROW 51005, 'Organization data validation failed: payment and member organizations differ.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.gift_records g
    INNER JOIN dbo.users boss ON boss.id = g.boss_user_id
    INNER JOIN dbo.users recipient ON recipient.id = g.recipient_user_id
    WHERE g.organization_id <> boss.organization_id
       OR g.organization_id <> recipient.organization_id
)
    THROW 51006, 'Organization data validation failed: gift record organization mismatch.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.orders o
    INNER JOIN dbo.users owner_user ON owner_user.id = o.owner_user_id
    WHERE o.organization_id <> owner_user.organization_id
)
    THROW 51008, 'Organization data validation failed: order owner organization mismatch.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.gift_records g
    INNER JOIN dbo.service_items s ON s.id = g.service_item_id
    WHERE g.organization_id <> s.organization_id
)
    THROW 51009, 'Organization data validation failed: gift item organization mismatch.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.audit_logs a
    INNER JOIN dbo.login_users lu ON lu.id = a.login_user_id
    WHERE a.organization_id <> lu.organization_id
)
    THROW 51010, 'Organization data validation failed: audit actor organization mismatch.', 1;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH('dbo.orders', 'created_audit_log_id') IS NULL
    ALTER TABLE dbo.orders ADD created_audit_log_id BIGINT NULL;
IF COL_LENGTH('dbo.orders', 'order_type') IS NULL
    ALTER TABLE dbo.orders ADD order_type NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_order_type DEFAULT N'boosting';
IF COL_LENGTH('dbo.orders', 'pricing_category') IS NULL
    ALTER TABLE dbo.orders ADD pricing_category NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.orders', 'service_quantity') IS NULL
    ALTER TABLE dbo.orders ADD service_quantity DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_service_quantity DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'base_amount') IS NULL
    ALTER TABLE dbo.orders ADD base_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_base_amount DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'designated_fee') IS NULL
    ALTER TABLE dbo.orders ADD designated_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_designated_fee DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'friend_fee') IS NULL
    ALTER TABLE dbo.orders ADD friend_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_friend_fee DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'replacement_fee') IS NULL
    ALTER TABLE dbo.orders ADD replacement_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_replacement_fee DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'night_fee') IS NULL
    ALTER TABLE dbo.orders ADD night_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_night_fee DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'other_fee') IS NULL
    ALTER TABLE dbo.orders ADD other_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_other_fee DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'discount_amount') IS NULL
    ALTER TABLE dbo.orders ADD discount_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_discount_amount DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'final_amount') IS NULL
    ALTER TABLE dbo.orders ADD final_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_final_amount DEFAULT 0;
IF COL_LENGTH('dbo.orders', 'activity_id') IS NULL
    ALTER TABLE dbo.orders ADD activity_id INT NULL;
IF COL_LENGTH('dbo.orders', 'activity_name_snapshot') IS NULL
    ALTER TABLE dbo.orders ADD activity_name_snapshot NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.orders', 'activity_discount_type') IS NULL
    ALTER TABLE dbo.orders ADD activity_discount_type NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.orders', 'activity_discount_value') IS NULL
    ALTER TABLE dbo.orders ADD activity_discount_value DECIMAL(10,2) NULL;
IF COL_LENGTH('dbo.orders', 'activity_include_fees') IS NULL
    ALTER TABLE dbo.orders ADD activity_include_fees BIT NOT NULL CONSTRAINT DF_orders_activity_include_fees DEFAULT 0;
EXEC(N'UPDATE dbo.orders
SET base_amount = amount,
    final_amount = amount
WHERE amount > 0
  AND base_amount = 0
  AND designated_fee = 0
  AND friend_fee = 0
  AND replacement_fee = 0
  AND night_fee = 0
  AND other_fee = 0
  AND discount_amount = 0
  AND final_amount = 0;');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_pricing_non_negative')
    EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_pricing_non_negative CHECK (base_amount >= 0 AND designated_fee >= 0 AND friend_fee >= 0 AND replacement_fee >= 0 AND night_fee >= 0 AND other_fee >= 0 AND discount_amount >= 0 AND final_amount >= 0)');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_discount_amount')
    EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_discount_amount CHECK (discount_amount <= base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee)');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_final_amount')
    EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_final_amount CHECK (final_amount = base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee - discount_amount)');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_orders_activity')
    EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT FK_orders_activity FOREIGN KEY (activity_id) REFERENCES dbo.activities(id)');
IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_activities_organization')
    EXEC(N'ALTER TABLE dbo.activities WITH CHECK ADD CONSTRAINT FK_activities_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_orders_activity_id' AND object_id = OBJECT_ID(N'dbo.orders'))
    CREATE INDEX IX_orders_activity_id ON dbo.orders(activity_id);
IF COL_LENGTH('dbo.orders', 'cancelled_audit_log_id') IS NULL
    ALTER TABLE dbo.orders ADD cancelled_audit_log_id BIGINT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_order_type')
    EXEC(N'ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CK_orders_order_type CHECK (order_type IN (N''boosting'', N''farming'', N''companion'', N''prepaid''))');
IF COL_LENGTH('dbo.payments', 'generated_audit_log_id') IS NULL
    ALTER TABLE dbo.payments ADD generated_audit_log_id BIGINT NULL;
IF COL_LENGTH('dbo.payments', 'paid_audit_log_id') IS NULL
    ALTER TABLE dbo.payments ADD paid_audit_log_id BIGINT NULL;
IF COL_LENGTH('dbo.gift_records', 'created_audit_log_id') IS NULL
    ALTER TABLE dbo.gift_records ADD created_audit_log_id BIGINT NULL;
IF COL_LENGTH('dbo.gift_records', 'cancelled_audit_log_id') IS NULL
    ALTER TABLE dbo.gift_records ADD cancelled_audit_log_id BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_orders_created_audit_log')
    ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT FK_orders_created_audit_log FOREIGN KEY (created_audit_log_id) REFERENCES dbo.audit_logs(id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_orders_cancelled_audit_log')
    ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT FK_orders_cancelled_audit_log FOREIGN KEY (cancelled_audit_log_id) REFERENCES dbo.audit_logs(id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_generated_audit_log')
    ALTER TABLE dbo.payments WITH CHECK ADD CONSTRAINT FK_payments_generated_audit_log FOREIGN KEY (generated_audit_log_id) REFERENCES dbo.audit_logs(id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_paid_audit_log')
    ALTER TABLE dbo.payments WITH CHECK ADD CONSTRAINT FK_payments_paid_audit_log FOREIGN KEY (paid_audit_log_id) REFERENCES dbo.audit_logs(id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gift_records_created_audit_log')
    ALTER TABLE dbo.gift_records WITH CHECK ADD CONSTRAINT FK_gift_records_created_audit_log FOREIGN KEY (created_audit_log_id) REFERENCES dbo.audit_logs(id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gift_records_cancelled_audit_log')
    ALTER TABLE dbo.gift_records WITH CHECK ADD CONSTRAINT FK_gift_records_cancelled_audit_log FOREIGN KEY (cancelled_audit_log_id) REFERENCES dbo.audit_logs(id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_orders_created_audit_log_id' AND object_id = OBJECT_ID(N'dbo.orders'))
    CREATE INDEX IX_orders_created_audit_log_id ON dbo.orders(created_audit_log_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_orders_cancelled_audit_log_id' AND object_id = OBJECT_ID(N'dbo.orders'))
    CREATE INDEX IX_orders_cancelled_audit_log_id ON dbo.orders(cancelled_audit_log_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payments_generated_audit_log_id' AND object_id = OBJECT_ID(N'dbo.payments'))
    CREATE INDEX IX_payments_generated_audit_log_id ON dbo.payments(generated_audit_log_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payments_paid_audit_log_id' AND object_id = OBJECT_ID(N'dbo.payments'))
    CREATE INDEX IX_payments_paid_audit_log_id ON dbo.payments(paid_audit_log_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gift_records_created_audit_log_id' AND object_id = OBJECT_ID(N'dbo.gift_records'))
    CREATE INDEX IX_gift_records_created_audit_log_id ON dbo.gift_records(created_audit_log_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gift_records_cancelled_audit_log_id' AND object_id = OBJECT_ID(N'dbo.gift_records'))
    CREATE INDEX IX_gift_records_cancelled_audit_log_id ON dbo.gift_records(cancelled_audit_log_id);

IF OBJECT_ID(N'dbo.TR_audit_logs_prevent_delete', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_audit_logs_prevent_delete ON dbo.audit_logs INSTEAD OF DELETE AS BEGIN SET NOCOUNT ON; THROW 51020, ''audit_logs is append-only and cannot be deleted.'', 1; END;');
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.money_logs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.money_logs
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_money_logs PRIMARY KEY,
        organization_id INT NOT NULL,
        user_id INT NOT NULL,
        login_user_id INT NULL,
        audit_log_id BIGINT NULL,
        reversed_money_log_id BIGINT NULL,
        type NVARCHAR(30) NOT NULL,
        amount DECIMAL(12,2) NOT NULL,
        balance_before DECIMAL(12,2) NOT NULL CONSTRAINT DF_money_logs_balance_before DEFAULT 0,
        balance_after DECIMAL(12,2) NOT NULL,
        status NVARCHAR(30) NOT NULL CONSTRAINT DF_money_logs_status DEFAULT N'completed',
        source_type NVARCHAR(50) NULL,
        source_id INT NULL,
        source_uuid UNIQUEIDENTIFIER NULL,
        note NVARCHAR(500) NULL,
        is_reversal BIT NOT NULL CONSTRAINT DF_money_logs_is_reversal DEFAULT 0,
        correlation_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_money_logs_correlation_id DEFAULT NEWID(),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_money_logs_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_money_logs_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_money_logs_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_money_logs_login_user FOREIGN KEY (login_user_id) REFERENCES dbo.login_users(id),
        CONSTRAINT FK_money_logs_audit_log FOREIGN KEY (audit_log_id) REFERENCES dbo.audit_logs(id),
        CONSTRAINT FK_money_logs_reversed_money_log FOREIGN KEY (reversed_money_log_id) REFERENCES dbo.money_logs(id)
    );
    CREATE INDEX IX_money_logs_user_id ON dbo.money_logs(user_id, id);
    CREATE INDEX IX_money_logs_source ON dbo.money_logs(source_type, source_id);
    CREATE INDEX IX_money_logs_created_at ON dbo.money_logs(created_at DESC);
    CREATE INDEX IX_money_logs_audit_log_id ON dbo.money_logs(audit_log_id);
    CREATE INDEX IX_money_logs_reversed_money_log_id ON dbo.money_logs(reversed_money_log_id);
    CREATE INDEX IX_money_logs_correlation_id ON dbo.money_logs(correlation_id);
END;

IF COL_LENGTH('dbo.money_logs', 'audit_log_id') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs ADD audit_log_id BIGINT NULL;
END;

IF COL_LENGTH('dbo.money_logs', 'balance_before') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs
        ADD balance_before DECIMAL(12,2) NOT NULL
            CONSTRAINT DF_money_logs_balance_before DEFAULT 0;
END;

IF COL_LENGTH('dbo.money_logs', 'status') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs
        ADD status NVARCHAR(30) NOT NULL
            CONSTRAINT DF_money_logs_status DEFAULT N'completed';
END;

UPDATE dbo.money_logs
SET balance_before = balance_after - amount
WHERE balance_before = 0
  AND balance_after <> amount;

IF COL_LENGTH('dbo.money_logs', 'reversed_money_log_id') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs ADD reversed_money_log_id BIGINT NULL;
END;

IF COL_LENGTH('dbo.money_logs', 'is_reversal') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs
        ADD is_reversal BIT NOT NULL
            CONSTRAINT DF_money_logs_is_reversal DEFAULT 0;
END;

IF COL_LENGTH('dbo.money_logs', 'correlation_id') IS NULL
BEGIN
    ALTER TABLE dbo.money_logs
        ADD correlation_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_money_logs_correlation_id DEFAULT NEWID();
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_money_logs_audit_log'
      AND parent_object_id = OBJECT_ID(N'dbo.money_logs')
)
BEGIN
    ALTER TABLE dbo.money_logs WITH CHECK
        ADD CONSTRAINT FK_money_logs_audit_log
        FOREIGN KEY (audit_log_id) REFERENCES dbo.audit_logs(id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_money_logs_reversed_money_log'
      AND parent_object_id = OBJECT_ID(N'dbo.money_logs')
)
BEGIN
    ALTER TABLE dbo.money_logs WITH CHECK
        ADD CONSTRAINT FK_money_logs_reversed_money_log
        FOREIGN KEY (reversed_money_log_id) REFERENCES dbo.money_logs(id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_money_logs_audit_log_id'
      AND object_id = OBJECT_ID(N'dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_audit_log_id
        ON dbo.money_logs(audit_log_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_money_logs_reversed_money_log_id'
      AND object_id = OBJECT_ID(N'dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_reversed_money_log_id
        ON dbo.money_logs(reversed_money_log_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_money_logs_correlation_id'
      AND object_id = OBJECT_ID(N'dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_correlation_id
        ON dbo.money_logs(correlation_id);
END;

IF OBJECT_ID(N'dbo.TR_money_logs_prevent_delete', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_money_logs_prevent_delete ON dbo.money_logs INSTEAD OF DELETE AS BEGIN SET NOCOUNT ON; THROW 51021, ''money_logs is append-only and cannot be deleted.'', 1; END;');
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.login_histories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.login_histories
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_login_histories PRIMARY KEY,
        organization_id INT NOT NULL,
        login_user_id INT NOT NULL,
        action NVARCHAR(30) NOT NULL,
        method NVARCHAR(30) NOT NULL,
        ip_address NVARCHAR(64) NULL,
        user_agent NVARCHAR(500) NULL,
        session_id NVARCHAR(120) NULL,
        device_info NVARCHAR(160) NULL,
        failure_reason NVARCHAR(160) NULL,
        succeeded BIT NOT NULL CONSTRAINT DF_login_histories_succeeded DEFAULT 1,
        logged_out_at DATETIME2 NULL,
        duration_seconds INT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_login_histories_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_login_histories_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_login_histories_login_user FOREIGN KEY (login_user_id) REFERENCES dbo.login_users(id)
    );
    CREATE INDEX IX_login_histories_login_user ON dbo.login_histories(login_user_id, created_at);
    CREATE INDEX IX_login_histories_created_at ON dbo.login_histories(created_at DESC);
END;

IF COL_LENGTH('dbo.login_histories', 'device_info') IS NULL
BEGIN
    ALTER TABLE dbo.login_histories ADD device_info NVARCHAR(160) NULL;
END;

IF COL_LENGTH('dbo.login_histories', 'failure_reason') IS NULL
BEGIN
    ALTER TABLE dbo.login_histories ADD failure_reason NVARCHAR(160) NULL;
END;

IF COL_LENGTH('dbo.login_histories', 'logged_out_at') IS NULL
BEGIN
    ALTER TABLE dbo.login_histories ADD logged_out_at DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.login_histories', 'duration_seconds') IS NULL
BEGIN
    ALTER TABLE dbo.login_histories ADD duration_seconds INT NULL;
END;

IF OBJECT_ID(N'dbo.file_attachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.file_attachments
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_file_attachments PRIMARY KEY,
        organization_id INT NOT NULL,
        target_type NVARCHAR(50) NOT NULL,
        target_id INT NOT NULL,
        target_uuid UNIQUEIDENTIFIER NULL,
        attachment_kind NVARCHAR(30) NULL,
        original_file_name NVARCHAR(255) NOT NULL,
        stored_file_name NVARCHAR(120) NOT NULL,
        storage_path NVARCHAR(500) NOT NULL,
        content_type NVARCHAR(120) NOT NULL,
        file_extension NVARCHAR(20) NULL,
        file_size BIGINT NOT NULL,
        sha256_hash CHAR(64) NULL,
        uploaded_by_login_user_id INT NULL,
        note NVARCHAR(500) NULL,
        is_deleted BIT NOT NULL CONSTRAINT DF_file_attachments_is_deleted DEFAULT 0,
        deleted_at DATETIME2 NULL,
        deleted_by_login_user_id INT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_file_attachments_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_file_attachments_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_file_attachments_uploaded_by FOREIGN KEY (uploaded_by_login_user_id) REFERENCES dbo.login_users(id),
        CONSTRAINT FK_file_attachments_deleted_by FOREIGN KEY (deleted_by_login_user_id) REFERENCES dbo.login_users(id)
    );
    CREATE INDEX IX_file_attachments_target ON dbo.file_attachments(organization_id, target_type, target_id, is_deleted, created_at);
    CREATE INDEX IX_file_attachments_target_uuid ON dbo.file_attachments(organization_id, target_type, target_uuid);
    CREATE INDEX IX_file_attachments_uploaded_by ON dbo.file_attachments(uploaded_by_login_user_id);
END;

IF COL_LENGTH('dbo.file_attachments', 'attachment_kind') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD attachment_kind NVARCHAR(30) NULL;
END;
IF COL_LENGTH('dbo.file_attachments', 'file_extension') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD file_extension NVARCHAR(20) NULL;
END;
IF COL_LENGTH('dbo.file_attachments', 'sha256_hash') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD sha256_hash CHAR(64) NULL;
END;
IF COL_LENGTH('dbo.file_attachments', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD is_deleted BIT NOT NULL CONSTRAINT DF_file_attachments_is_deleted DEFAULT 0;
END;
IF COL_LENGTH('dbo.file_attachments', 'deleted_at') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD deleted_at DATETIME2 NULL;
END;
IF COL_LENGTH('dbo.file_attachments', 'deleted_by_login_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments ADD deleted_by_login_user_id INT NULL;
END;
IF OBJECT_ID(N'dbo.FK_file_attachments_deleted_by', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.file_attachments
    ADD CONSTRAINT FK_file_attachments_deleted_by FOREIGN KEY (deleted_by_login_user_id) REFERENCES dbo.login_users(id);
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_file_attachments_target_uuid' AND object_id = OBJECT_ID(N'dbo.file_attachments'))
BEGIN
    CREATE INDEX IX_file_attachments_target_uuid ON dbo.file_attachments(organization_id, target_type, target_uuid);
END;
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.user_preferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.user_preferences
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_user_preferences PRIMARY KEY,
        login_user_id INT NOT NULL,
        theme_name NVARCHAR(50) NOT NULL CONSTRAINT DF_user_preferences_theme_name DEFAULT N'internal-ops',
        accent_color NVARCHAR(20) NULL,
        dashboard_layout NVARCHAR(MAX) NULL,
        table_page_size INT NOT NULL CONSTRAINT DF_user_preferences_table_page_size DEFAULT 100,
        default_order_status_filter NVARCHAR(30) NULL,
        default_money_log_filter NVARCHAR(30) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_user_preferences_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL,
        CONSTRAINT FK_user_preferences_login_user FOREIGN KEY (login_user_id) REFERENCES dbo.login_users(id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UQ_user_preferences_login_user ON dbo.user_preferences(login_user_id);
END;

INSERT INTO dbo.user_preferences(login_user_id, theme_name, table_page_size, created_at)
SELECT lu.id, N'internal-ops', 100, SYSUTCDATETIME()
FROM dbo.login_users lu
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.user_preferences existing
    WHERE existing.login_user_id = lu.id
);

UPDATE dbo.user_preferences
SET theme_name = N'internal-ops',
    updated_at = SYSUTCDATETIME()
WHERE theme_name = N'purple-tech';
""");
    }
}
