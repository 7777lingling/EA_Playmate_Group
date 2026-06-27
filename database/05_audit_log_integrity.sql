SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.audit_logs', 'batch_uuid') IS NULL
    ALTER TABLE dbo.audit_logs ADD batch_uuid UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_audit_logs_batch_uuid'
      AND object_id = OBJECT_ID(N'dbo.audit_logs')
)
    CREATE INDEX IX_audit_logs_batch_uuid ON dbo.audit_logs(batch_uuid);

IF COL_LENGTH('dbo.orders', 'created_audit_log_id') IS NULL
    ALTER TABLE dbo.orders ADD created_audit_log_id BIGINT NULL;
IF COL_LENGTH('dbo.orders', 'cancelled_audit_log_id') IS NULL
    ALTER TABLE dbo.orders ADD cancelled_audit_log_id BIGINT NULL;
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

IF OBJECT_ID(N'dbo.TR_money_logs_prevent_delete', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_money_logs_prevent_delete ON dbo.money_logs INSTEAD OF DELETE AS BEGIN SET NOCOUNT ON; THROW 51021, ''money_logs is append-only and cannot be deleted.'', 1; END;');

COMMIT TRANSACTION;
