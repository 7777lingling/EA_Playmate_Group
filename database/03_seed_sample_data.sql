USE [EAPlaymateGroup];
GO

INSERT INTO dbo.users (nickname, system_role, is_player, is_boss)
SELECT N'DemoPlayerA', N'staff', 1, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.users WHERE nickname = N'DemoPlayerA');

INSERT INTO dbo.users (nickname, system_role, is_player, is_boss)
SELECT N'DemoPlayerB', N'staff', 1, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.users WHERE nickname = N'DemoPlayerB');

INSERT INTO dbo.users (nickname, system_role, is_player, is_boss)
SELECT N'DemoBoss', N'staff', 0, 1
WHERE NOT EXISTS (SELECT 1 FROM dbo.users WHERE nickname = N'DemoBoss');
GO

DECLARE @order_id INT;
DECLARE @owner_user_id INT = (SELECT id FROM dbo.users WHERE nickname = N'DemoBoss');
DECLARE @player_a_user_id INT = (SELECT id FROM dbo.users WHERE nickname = N'DemoPlayerA');
DECLARE @player_b_user_id INT = (SELECT id FROM dbo.users WHERE nickname = N'DemoPlayerB');

IF NOT EXISTS (SELECT 1 FROM dbo.orders WHERE order_no = N'DEMO-001')
BEGIN
    INSERT INTO dbo.orders (
        order_no,
        order_date,
        owner_user_id,
        amount,
        commission_rate,
        commission_amount,
        status,
        customer_payment_status,
        remark
    )
    VALUES (
        N'DEMO-001',
        CONVERT(date, GETDATE()),
        @owner_user_id,
        200.00,
        0.1000,
        20.00,
        N'completed',
        N'unpaid',
        N'Demo order. You can delete it after verification.'
    );

    SET @order_id = SCOPE_IDENTITY();

    INSERT INTO dbo.order_members (order_id, user_id, role, share_amount)
    VALUES
        (@order_id, @player_a_user_id, N'player', 90.00),
        (@order_id, @player_b_user_id, N'player', 90.00);
END
GO
