using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task ValidateOrganizationFiltersAsync(EAPlaymateGroupDbContext db)
    {
        await db.LoginUsers.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Users.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Orders.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.OrderMembers.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.Payments.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.AuditLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.MoneyLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
        await db.ServiceItems.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync();
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
        (N'viewer', N'Member.View', 1),
        (N'viewer', N'Gift.View', 1),
        (N'viewer', N'Order.View', 1)
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

IF COL_LENGTH('dbo.audit_logs', 'correlation_id') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs
        ADD correlation_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_audit_logs_correlation_id DEFAULT NEWID();
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
        CONSTRAINT CK_service_items_category CHECK ([category] IN (N'boost', N'grind', N'play', N'gift', N'deposit_bonus', N'other'))
    );

    CREATE UNIQUE INDEX UQ_service_items_uuid ON dbo.service_items(uuid);
    CREATE UNIQUE INDEX UQ_service_items_seed_key ON dbo.service_items(seed_key);
    CREATE INDEX IX_service_items_category_sort ON dbo.service_items(category, sort_order);
END;

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

        (N'play-entertainment', N'play', N'娛樂陪', N'陪玩 - 娛樂陪', N'hour_person', CAST(100 AS DECIMAL(12,2)), N'100 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 300),
        (N'play-technical-tier-1-3', N'play', N'技術陪', N'陪玩 - 技術陪 一至三階', N'hour_person', CAST(150 AS DECIMAL(12,2)), N'150 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 310),
        (N'play-technical-tier-4', N'play', N'技術陪', N'陪玩 - 技術陪 四階', N'hour_person', CAST(180 AS DECIMAL(12,2)), N'180 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 320),
        (N'play-technical-tier-5', N'play', N'技術陪', N'陪玩 - 技術陪 五階', N'hour_person', CAST(210 AS DECIMAL(12,2)), N'210 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 330),
        (N'play-technical-tier-6', N'play', N'技術陪', N'陪玩 - 技術陪 六階', N'hour_person', CAST(230 AS DECIMAL(12,2)), N'230 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 340),
        (N'play-technical-tier-7', N'play', N'技術陪', N'陪玩 - 技術陪 七階', N'hour_person', CAST(250 AS DECIMAL(12,2)), N'250 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 350),
        (N'play-technical-tier-peak7', N'play', N'技術陪', N'陪玩 - 技術陪 巔七以上', N'hour_person', CAST(300 AS DECIMAL(12,2)), N'300 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50；40 以上暫不接。', 360),
        (N'play-teaching', N'play', N'教學陪', N'陪玩 - 教學陪', N'hour_person', CAST(100 AS DECIMAL(12,2)), N'100 / 小時 / 人', N'指定陪玩 +10 / 人；帶一個朋友 +50。', 370),

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
);

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
);
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
);
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
    CREATE UNIQUE INDEX UQ_organizations_name ON dbo.organizations(name);
END;

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
""");

        await db.Database.ExecuteSqlRawAsync("""
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
        balance_after DECIMAL(12,2) NOT NULL,
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
""");
    }
}
