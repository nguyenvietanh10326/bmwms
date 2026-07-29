-- ============================================================
-- SEED: Tài khoản Admin mặc định cho BMWMS
-- Password: Admin@123
-- ============================================================

-- 1. Thêm Role ADMIN (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'ADMIN')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName, Description, IsSystemRole, IsActive, CreatedAt)
    VALUES ('ADMIN', 'Quản trị hệ thống', 'Toàn quyền quản trị hệ thống', 1, 1, GETUTCDATE());
    PRINT 'Role ADMIN created.';
END

-- 2. Thêm Role WH_MANAGER (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'WH_MANAGER')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName, Description, IsSystemRole, IsActive, CreatedAt)
    VALUES ('WH_MANAGER', 'Quản lý kho', 'Quản lý hoạt động kho hàng', 0, 1, GETUTCDATE());
    PRINT 'Role WH_MANAGER created.';
END

-- 3. Thêm Role WH_STAFF (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'WH_STAFF')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName, Description, IsSystemRole, IsActive, CreatedAt)
    VALUES ('WH_STAFF', 'Nhân viên kho', 'Thực hiện nhập/xuất kho', 0, 1, GETUTCDATE());
    PRINT 'Role WH_STAFF created.';
END

-- 4. Tạo user admin (nếu chưa có)
DECLARE @AdminRoleId INT = (SELECT RoleId FROM Roles WHERE RoleCode = 'ADMIN');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (
        RoleId, Username, Email, PasswordHash,
        FullName, PhoneNumber, Status,
        FailedLoginCount, CreatedAt
    )
    VALUES (
        @AdminRoleId,
        'admin',
        'admin@bmwms.local',
        '$2a$12$FvO.PtJ54IERqWFKb.kmqOdQ176rBi6TTS2PIjvUPlfBy/7NUPlUq',
        N'Quản trị viên hệ thống',
        '0900000000',
        'ACTIVE',
        0,
        GETUTCDATE()
    );
    PRINT 'Admin user created. Username: admin | Password: Admin@123';
END
ELSE
BEGIN
    PRINT 'Admin user already exists.';
END

-- Hiển thị kết quả
SELECT u.UserId, u.Username, u.Email, u.FullName, u.Status, r.RoleName
FROM Users u
JOIN Roles r ON u.RoleId = r.RoleId
WHERE u.Username = 'admin';
