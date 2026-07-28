-- =====================================================
-- Script: Seed dữ liệu test cho UC52 - Xem danh sách người dùng
-- Password chung cho TẤT CẢ tài khoản: Admin@123456
-- =====================================================

-- 1. Cập nhật tài khoản admin hiện tại: đổi RoleID từ 6 (ADMIN cũ) sang 3 (SYSTEM_ADMIN)
UPDATE [dbo].[Users] 
SET RoleId = 3,
    PasswordHash = '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO',
    FailedLoginCount = 0,
    LockedUntil = NULL,
    UpdatedAt = GETUTCDATE()
WHERE UserId = 1;

-- 2. Thêm các tài khoản test
-- Kiểm tra trước để không bị lỗi trùng
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'nguyen.van.an')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (4, 'nguyen.van.an', 'nguyen.van.an@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Nguyễn Văn An', '0901000001', NULL, 'ACTIVE', 0, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'tran.thi.binh')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (1, 'tran.thi.binh', 'tran.thi.binh@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Trần Thị Bình', '0901000002', NULL, 'ACTIVE', 0, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'pham.thi.cuc')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (5, 'pham.thi.cuc', 'pham.thi.cuc@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Phạm Thị Cúc', '0901000003', NULL, 'ACTIVE', 0, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'hoang.minh')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (2, 'hoang.minh', 'hoang.minh@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Hoàng Minh', '0901000004', NULL, 'ACTIVE', 0, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'le.van.dat')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (5, 'le.van.dat', 'le.van.dat@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Lê Văn Đạt', '0901000005', NULL, 'LOCKED', 0, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Username = 'vo.thi.em')
BEGIN
    INSERT INTO [dbo].[Users] (RoleId, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
    VALUES (4, 'vo.thi.em', 'vo.thi.em@bmwms.vn', '$2a$12$TEkCvxw3rgc7YeJ596LJS.NLq0.KfFBBz2J/tdiA8C50JcNUmhVBO', N'Võ Thị Em', '0901000006', NULL, 'ACTIVE', 0, GETUTCDATE());
END

-- 3. Vô hiệu hóa 3 role cũ (nếu chưa)
UPDATE [dbo].[Roles] SET IsActive = 0 WHERE RoleCode IN ('ADMIN', 'WH_MANAGER', 'WH_STAFF');

-- 4. Kiểm tra kết quả
SELECT u.UserId, u.Username, u.FullName, u.Email, u.Status, r.RoleCode, r.RoleName
FROM [dbo].[Users] u
INNER JOIN [dbo].[Roles] r ON u.RoleId = r.RoleID
ORDER BY u.UserId;
