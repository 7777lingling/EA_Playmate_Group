USE [EAPlaymateGroup];
GO

CREATE TABLE dbo.users (
    id INT IDENTITY(1,1) NOT NULL,
    uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_users_uuid DEFAULT NEWID(),

    nickname NVARCHAR(50) NOT NULL,
    discord_id NVARCHAR(50) NULL,
    discord_name NVARCHAR(100) NULL,
    bank_account NVARCHAR(200) NULL,

    system_role NVARCHAR(20) NOT NULL CONSTRAINT DF_users_system_role DEFAULT N'staff',
    is_player BIT NOT NULL CONSTRAINT DF_users_is_player DEFAULT 1,
    is_boss BIT NOT NULL CONSTRAINT DF_users_is_boss DEFAULT 0,

    is_active BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT 1,
    left_at DATETIME2 NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,

    CONSTRAINT PK_users PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_users_uuid UNIQUE (uuid),
    CONSTRAINT UQ_users_nickname UNIQUE (nickname),
    CONSTRAINT CK_users_system_role CHECK (system_role IN (N'admin', N'staff', N'viewer'))
);
GO

CREATE UNIQUE INDEX UQ_users_discord_id
ON dbo.users (discord_id)
WHERE discord_id IS NOT NULL;
GO

CREATE TABLE dbo.activities (
    id INT IDENTITY(1,1) NOT NULL,
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

    CONSTRAINT PK_activities PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_activities_uuid UNIQUE (uuid),
    CONSTRAINT CK_activities_period CHECK (ends_at >= starts_at),
    CONSTRAINT CK_activities_discount_type CHECK (discount_type IN (N'percent', N'fixed_amount', N'fixed_price')),
    CONSTRAINT CK_activities_discount_value CHECK (discount_value >= 0 AND (discount_type <> N'percent' OR discount_value <= 100))
);
GO

CREATE INDEX IX_activities_scope
ON dbo.activities (organization_id, is_active, starts_at, ends_at);
GO

CREATE TABLE dbo.orders (
    id INT IDENTITY(1,1) NOT NULL,
    uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_orders_uuid DEFAULT NEWID(),

    order_no NVARCHAR(30) NULL,
    order_type NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_order_type DEFAULT N'boosting',
    pricing_category NVARCHAR(50) NULL,
    order_date DATE NOT NULL,

    owner_user_id INT NULL,
    amount DECIMAL(12,2) NOT NULL,
    service_quantity DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_service_quantity DEFAULT 0,
    base_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_base_amount DEFAULT 0,
    designated_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_designated_fee DEFAULT 0,
    friend_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_friend_fee DEFAULT 0,
    replacement_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_replacement_fee DEFAULT 0,
    night_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_night_fee DEFAULT 0,
    other_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_other_fee DEFAULT 0,
    discount_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_discount_amount DEFAULT 0,
    final_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_final_amount DEFAULT 0,
    activity_id INT NULL,
    activity_name_snapshot NVARCHAR(100) NULL,
    activity_discount_type NVARCHAR(30) NULL,
    activity_discount_value DECIMAL(10,2) NULL,
    activity_include_fees BIT NOT NULL CONSTRAINT DF_orders_activity_include_fees DEFAULT 0,
    commission_rate DECIMAL(6,4) NOT NULL CONSTRAINT DF_orders_commission_rate DEFAULT 0,
    commission_amount DECIMAL(12,2) NOT NULL,

    status NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_status DEFAULT N'completed',
    customer_payment_status NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_customer_payment_status DEFAULT N'unpaid',

    remark NVARCHAR(500) NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_orders_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,

    CONSTRAINT PK_orders PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_orders_uuid UNIQUE (uuid),
    CONSTRAINT UQ_orders_order_no UNIQUE (order_no),
    CONSTRAINT FK_orders_owner_user FOREIGN KEY (owner_user_id) REFERENCES dbo.users(id),
    CONSTRAINT FK_orders_activity FOREIGN KEY (activity_id) REFERENCES dbo.activities(id),
    CONSTRAINT CK_orders_amount CHECK (amount >= 0),
    CONSTRAINT CK_orders_pricing_non_negative CHECK (
        base_amount >= 0
        AND designated_fee >= 0
        AND friend_fee >= 0
        AND replacement_fee >= 0
        AND night_fee >= 0
        AND other_fee >= 0
        AND discount_amount >= 0
        AND final_amount >= 0
    ),
    CONSTRAINT CK_orders_discount_amount CHECK (discount_amount <= base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee),
    CONSTRAINT CK_orders_final_amount CHECK (final_amount = base_amount + designated_fee + friend_fee + replacement_fee + night_fee + other_fee - discount_amount),
    CONSTRAINT CK_orders_commission_rate CHECK (commission_rate >= 0 AND commission_rate <= 1),
    CONSTRAINT CK_orders_commission_amount CHECK (commission_amount >= 0),
    CONSTRAINT CK_orders_order_type CHECK (order_type IN (N'boosting', N'farming', N'companion', N'prepaid')),
    CONSTRAINT CK_orders_status CHECK (status IN (N'draft', N'completed', N'cancelled', N'disputed')),
    CONSTRAINT CK_orders_customer_payment_status CHECK (customer_payment_status IN (N'unpaid', N'partial', N'paid', N'refunded'))
);
GO

CREATE TABLE dbo.order_members (
    id INT IDENTITY(1,1) NOT NULL,

    order_id INT NOT NULL,
    user_id INT NOT NULL,

    role NVARCHAR(20) NOT NULL CONSTRAINT DF_order_members_role DEFAULT N'player',
    share_amount DECIMAL(12,2) NOT NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_order_members_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_order_members PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_order_members_order FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT FK_order_members_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
    CONSTRAINT UQ_order_members_order_user_role UNIQUE (order_id, user_id, role),
    CONSTRAINT CK_order_members_role CHECK (role IN (N'player', N'leader', N'trainer', N'bonus')),
    CONSTRAINT CK_order_members_share_amount CHECK (share_amount >= 0)
);
GO

CREATE TABLE dbo.payments (
    id INT IDENTITY(1,1) NOT NULL,
    uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_payments_uuid DEFAULT NEWID(),

    user_id INT NOT NULL,
    pay_month CHAR(7) NOT NULL,

    expected_amount DECIMAL(12,2) NOT NULL,
    actual_amount DECIMAL(12,2) NULL,
    payment_status NVARCHAR(20) NOT NULL CONSTRAINT DF_payments_payment_status DEFAULT N'pending',

    snapshot_json NVARCHAR(MAX) NULL,

    paid_at DATETIME2 NULL,
    note NVARCHAR(500) NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_payments_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,

    CONSTRAINT PK_payments PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_payments_uuid UNIQUE (uuid),
    CONSTRAINT UQ_payments_user_month UNIQUE (user_id, pay_month),
    CONSTRAINT FK_payments_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
    CONSTRAINT CK_payments_pay_month CHECK (pay_month LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]'),
    CONSTRAINT CK_payments_expected_amount CHECK (expected_amount >= 0),
    CONSTRAINT CK_payments_actual_amount CHECK (actual_amount IS NULL OR actual_amount >= 0),
    CONSTRAINT CK_payments_payment_status CHECK (payment_status IN (N'pending', N'paid', N'cancelled'))
);
GO

CREATE TABLE dbo.audit_logs (
    id BIGINT IDENTITY(1,1) NOT NULL,

    user_id INT NULL,
    login_user_id INT NULL,
    action NVARCHAR(50) NOT NULL,
    target_type NVARCHAR(50) NOT NULL,
    target_id INT NULL,
    target_uuid UNIQUEIDENTIFIER NULL,

    before_json NVARCHAR(MAX) NULL,
    after_json NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_audit_logs_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_audit_logs PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_audit_logs_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
);
GO

CREATE INDEX IX_orders_order_date_status
ON dbo.orders (order_date, status)
INCLUDE (amount, commission_amount, customer_payment_status);
GO

CREATE INDEX IX_orders_customer_payment_status
ON dbo.orders (customer_payment_status, status, order_date);
GO

CREATE INDEX IX_orders_activity_id
ON dbo.orders (activity_id);
GO

CREATE INDEX IX_order_members_user_order
ON dbo.order_members (user_id, order_id)
INCLUDE (share_amount, role);
GO

CREATE INDEX IX_payments_pay_month
ON dbo.payments (pay_month, payment_status);
GO

CREATE INDEX IX_audit_logs_target
ON dbo.audit_logs (target_type, target_id, created_at);
GO

CREATE INDEX IX_audit_logs_login_user
ON dbo.audit_logs (login_user_id, created_at);
GO

CREATE TABLE dbo.file_attachments (
    id BIGINT IDENTITY(1,1) NOT NULL,
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

    CONSTRAINT PK_file_attachments PRIMARY KEY CLUSTERED (id)
);
GO

CREATE INDEX IX_file_attachments_target
ON dbo.file_attachments (organization_id, target_type, target_id, is_deleted, created_at);
GO

CREATE INDEX IX_file_attachments_target_uuid
ON dbo.file_attachments (organization_id, target_type, target_uuid);
GO

CREATE INDEX IX_file_attachments_uploaded_by
ON dbo.file_attachments (uploaded_by_login_user_id);
GO
