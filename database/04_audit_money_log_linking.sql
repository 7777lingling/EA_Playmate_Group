SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.audit_logs', 'correlation_id') IS NULL
BEGIN
    ALTER TABLE dbo.audit_logs
        ADD correlation_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_audit_logs_correlation_id DEFAULT NEWID();
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
    WHERE name = 'FK_money_logs_audit_log'
      AND parent_object_id = OBJECT_ID('dbo.money_logs')
)
BEGIN
    ALTER TABLE dbo.money_logs WITH CHECK
        ADD CONSTRAINT FK_money_logs_audit_log
        FOREIGN KEY (audit_log_id) REFERENCES dbo.audit_logs(id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_money_logs_reversed_money_log'
      AND parent_object_id = OBJECT_ID('dbo.money_logs')
)
BEGIN
    ALTER TABLE dbo.money_logs WITH CHECK
        ADD CONSTRAINT FK_money_logs_reversed_money_log
        FOREIGN KEY (reversed_money_log_id) REFERENCES dbo.money_logs(id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_audit_logs_correlation_id'
      AND object_id = OBJECT_ID('dbo.audit_logs')
)
BEGIN
    CREATE INDEX IX_audit_logs_correlation_id
        ON dbo.audit_logs(correlation_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_money_logs_audit_log_id'
      AND object_id = OBJECT_ID('dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_audit_log_id
        ON dbo.money_logs(audit_log_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_money_logs_reversed_money_log_id'
      AND object_id = OBJECT_ID('dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_reversed_money_log_id
        ON dbo.money_logs(reversed_money_log_id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_money_logs_correlation_id'
      AND object_id = OBJECT_ID('dbo.money_logs')
)
BEGIN
    CREATE INDEX IX_money_logs_correlation_id
        ON dbo.money_logs(correlation_id);
END;

COMMIT TRANSACTION;
