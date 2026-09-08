/*
    Bổ sung tài khoản nhân viên kho cho mô hình một nhân viên - một nhiệm vụ.
    Script chạy lặp lại an toàn: không tạo trùng Username hoặc Email.

    Mật khẩu khởi tạo: Admin@123456
    Yêu cầu đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
*/
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @WarehouseStaffRoleID INT =
    (
        SELECT RoleID
        FROM dbo.Roles
        WHERE RoleCode = 'WAREHOUSE_STAFF' AND IsActive = 1
    );

    IF @WarehouseStaffRoleID IS NULL
        THROW 52010, N'Không tìm thấy vai trò WAREHOUSE_STAFF đang hoạt động.', 1;

    DECLARE @DefaultPasswordHash VARCHAR(500) =
        '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.';

    DECLARE @Staff TABLE
    (
        Username    VARCHAR(100)  NOT NULL,
        Email       VARCHAR(255)  NOT NULL,
        FullName    NVARCHAR(250) NOT NULL,
        PhoneNumber VARCHAR(20)   NULL
    );

    INSERT @Staff (Username, Email, FullName, PhoneNumber)
    VALUES
        ('staff.kho.01', 'staff.kho.01@bmwms.vn', N'Nguyễn Minh Khang - Nhân viên kho', '0900000101'),
        ('staff.kho.02', 'staff.kho.02@bmwms.vn', N'Trần Hoàng Nam - Nhân viên kho',   '0900000102'),
        ('staff.kho.03', 'staff.kho.03@bmwms.vn', N'Lê Quốc Bảo - Nhân viên kho',      '0900000103'),
        ('staff.kho.04', 'staff.kho.04@bmwms.vn', N'Phạm Gia Huy - Nhân viên kho',     '0900000104');

    INSERT dbo.Users
    (
        RoleID, Username, Email, PasswordHash, FullName, PhoneNumber,
        Status, FailedLoginCount, CreatedAt
    )
    SELECT
        @WarehouseStaffRoleID, s.Username, s.Email, @DefaultPasswordHash,
        s.FullName, s.PhoneNumber, 'ACTIVE', 0, SYSUTCDATETIME()
    FROM @Staff s
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users u
        WHERE u.Username = s.Username OR u.Email = s.Email
    );

    SELECT
        u.UserID,
        u.Username,
        u.FullName,
        u.Status,
        r.RoleCode
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleID = u.RoleID
    WHERE u.Username IN ('staff.kho.01','staff.kho.02','staff.kho.03','staff.kho.04')
    ORDER BY u.Username;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
