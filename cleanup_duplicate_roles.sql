-- ============================================================
-- Dọn dẹp 3 Role trùng: ADMIN, WH_MANAGER, WH_STAFF
-- Chuyển user đang dùng Role cũ sang Role chính thức
-- ============================================================

-- 1. Chuyển user đang dùng ADMIN (RoleID=6) → SYSTEM_ADMIN (RoleID=3)
UPDATE [dbo].[Users]
SET [RoleID] = 3
WHERE [RoleID] = 6;

-- 2. Chuyển user đang dùng WH_MANAGER (RoleID=7) → WAREHOUSE_MANAGER (RoleID=4)
UPDATE [dbo].[Users]
SET [RoleID] = 4
WHERE [RoleID] = 7;

-- 3. Chuyển user đang dùng WH_STAFF (RoleID=8) → WAREHOUSE_STAFF (RoleID=5)
UPDATE [dbo].[Users]
SET [RoleID] = 5
WHERE [RoleID] = 8;

-- 4. Deactivate 3 Role trùng
UPDATE [dbo].[Roles]
SET [IsActive] = 0
WHERE [RoleID] IN (6, 7, 8);

PRINT N'Đã dọn dẹp 3 Role trùng thành công.';
GO
