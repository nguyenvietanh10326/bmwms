IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [UserPasswordHistories] (
    [HistoryId] bigint NOT NULL IDENTITY,
    [UserID] bigint NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2(0) NOT NULL DEFAULT ((sysutcdatetime())),
    CONSTRAINT [PK__UserPass__4D7B4ADD5A11F68C] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_UserPasswordHistories_User] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);

CREATE INDEX [IX_UserPasswordHistories_UserID] ON [UserPasswordHistories] ([UserID]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260727183927_AddUserPasswordHistory', N'9.0.8');

COMMIT;
GO

