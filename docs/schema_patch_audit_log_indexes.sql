/*
    BMWMS - Audit log query indexes
    Idempotent patch: run once on an existing BMWMS database before enabling
    the Audit Log administration screen on a database with substantial history.
*/

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
    THROW 51000, N'Không tìm thấy bảng dbo.AuditLogs. Hãy chạy schema BMWMS nền trước.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt DESC, AuditLogID DESC)
        INCLUDE (UserID, ActionType, EntityName, EntityID, IpAddress);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_UserDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_UserDate ON dbo.AuditLogs(UserID, CreatedAt DESC, AuditLogID DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_EntityDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_EntityDate ON dbo.AuditLogs(EntityName, EntityID, CreatedAt DESC, AuditLogID DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ActionDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_ActionDate ON dbo.AuditLogs(ActionType, CreatedAt DESC, AuditLogID DESC);
GO
