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
    }
}
