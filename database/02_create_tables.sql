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

CREATE TABLE dbo.orders (
    id INT IDENTITY(1,1) NOT NULL,
    uuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_orders_uuid DEFAULT NEWID(),

    order_no NVARCHAR(30) NULL,
    order_date DATE NOT NULL,

    owner_user_id INT NULL,
    amount DECIMAL(12,2) NOT NULL,
    commission_rate DECIMAL(6,4) NOT NULL CONSTRAINT DF_orders_commission_rate DEFAULT 0.1000,
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
    CONSTRAINT CK_orders_amount CHECK (amount >= 0),
    CONSTRAINT CK_orders_commission_rate CHECK (commission_rate >= 0 AND commission_rate <= 1),
    CONSTRAINT CK_orders_commission_amount CHECK (commission_amount >= 0),
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
