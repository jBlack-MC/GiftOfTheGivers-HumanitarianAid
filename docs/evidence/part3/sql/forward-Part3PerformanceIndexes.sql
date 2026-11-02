BEGIN TRANSACTION;
ALTER TABLE [ReliefProjects] DROP CONSTRAINT [CK_ReliefProject_Status];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReliefProjects]') AND [c].[name] = N'Status');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [ReliefProjects] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [ReliefProjects] ALTER COLUMN [Status] nvarchar(20) NOT NULL;

ALTER TABLE [ReliefProjects] ADD CONSTRAINT [CK_ReliefProject_Status] CHECK ([Status] IN ('Active','Completed','Suspended'));

CREATE INDEX [IX_ReliefProjects_Status_CreatedDate] ON [ReliefProjects] ([Status], [CreatedDate]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261102132424_Part3PerformanceIndexes', N'10.0.14');

COMMIT;
GO

