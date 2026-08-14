-- ==============================================================================
-- BMWMS - BẬT LẠI TOÀN BỘ TRIGGER VÀ KIỂM TRA KHÓA NGOẠI (ENABLE ALL TRIGGERS)
-- ==============================================================================

USE [BMWMS];
GO

SET NOCOUNT ON;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 1: BẬT LẠI TOÀN BỘ TRIGGER TRÊN TẤT CẢ CÁC BẢNG';
PRINT N'======================================================================';

EXEC sp_MSforeachtable 'ALTER TABLE ? ENABLE TRIGGER ALL';
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 2: BẬT LẠI VÀ KIỂM TRA RÀNG BUỘC KHÓA NGOẠI (FOREIGN KEYS)';
PRINT N'======================================================================';

EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

PRINT N'======================================================================';
PRINT N'>>> TỔNG HỢP TRẠNG THÁI TRIGGER HIỆN TẠI TRONG DATABASE BMWMS:';
PRINT N'======================================================================';

SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    CASE WHEN t.is_disabled = 1 THEN N'❌ DISABELD (Đã tắt)' ELSE N'✅ ENABLED (Đã bật)' END AS Status
FROM sys.triggers t
ORDER BY TableName, TriggerName;
GO

PRINT N'======================================================================';
PRINT N'>>> ĐÃ BẬT LẠI TOÀN BỘ TRIGGER VÀ CHECK CONSTRAINT THÀNH CÔNG!';
PRINT N'======================================================================';
GO
